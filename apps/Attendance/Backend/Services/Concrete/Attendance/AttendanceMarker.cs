using AutoMapper;

using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Remain;
using Backend.Hubs;
using Backend.Options;
using Backend.Results.Attendance;
using Backend.Services.Abstract.Attendance;
using Backend.Transporters.Entities;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Backend.Services.Concrete.Attendance;

public sealed class AttendanceMarker(IClaimContext claimContext,
                                     IUnitOfWork unitOfWork,
                                     IStudentRemainClassesDao remainClassesDao,
                                     IHubContext<AttendanceHub> hubContext,
                                     IMapper mapper,
                                     IOptions<AttendanceOptions> attendanceOptions,
                                     ILogger<AttendanceMarker> logger) : IAttendanceMarker
{
    private readonly AttendanceOptions _attendanceOptions = attendanceOptions.Value;

    public async Task<MarkAttendanceOutcome> MarkAsync<TEntity, TResponse>(
        Func<AttendanceMarkContext, Task<AttendanceBuildResult<TEntity>?>> resolveAndBuildAttendance,
        Func<TEntity, ITransactionContext, Task<int>> countOtherStudentsAsync,
        Func<TEntity, ITransactionContext, Task<bool>> tryMarkAttendanceAsync,
        Func<TEntity, string> resolveBroadcastGroup)
        where TEntity : class
    {
        string tenantTimezoneId = claimContext.TenantTimezone;
        if (!AttendanceTimeWindow.TryGetIsNowInside(
                tenantTimezoneId,
                _attendanceOptions.AllowedWindowStart,
                _attendanceOptions.AllowedWindowEnd,
                out bool isWithinWindow))
        {
            logger.LogWarning(
                "Tenant timezone '{TimezoneId}' inválido — rechazando marcaje de asistencia",
                tenantTimezoneId);
            return new MarkAttendanceOutcome.InvalidTenantTimezone();
        }

        if (!isWithinWindow)
        {
            return new MarkAttendanceOutcome.OutsideAllowedWindow();
        }

        AttendanceMarkContext markContext = new AttendanceMarkContext(
            claimContext.TenantId,
            claimContext.UserId,
            claimContext.UserName,
            tenantTimezoneId);

        AttendanceBuildResult<TEntity>? buildResult = await resolveAndBuildAttendance(markContext);
        if (buildResult is null)
        {
            return new MarkAttendanceOutcome.InvalidClass();
        }

        TEntity attendance = buildResult.Attendance;
        int maxStudentLimit = buildResult.MaxStudentLimit;

        MarkAttendanceOutcome result = await unitOfWork.RunInTransactionAsync(transaction =>
            AttendanceRecording.TryRecordAsync(
                markContext.TenantId,
                markContext.StudentId,
                remainClassesDao,
                maxStudentLimit,
                markTransaction => countOtherStudentsAsync(attendance, markTransaction),
                markTransaction => tryMarkAttendanceAsync(attendance, markTransaction),
                transaction));

        if (result is MarkAttendanceOutcome.Marked)
        {
            TResponse response = mapper.Map<TResponse>(attendance);
            await hubContext.Clients
                            .Group(resolveBroadcastGroup(attendance))
                            .SendAsync("AttendanceMarked", response);
        }

        return result;
    }
}
