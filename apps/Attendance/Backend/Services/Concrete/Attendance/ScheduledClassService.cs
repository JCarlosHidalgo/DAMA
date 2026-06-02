using AutoMapper;

using Backend.Builders;
using Backend.Claims;
using Backend.Common;
using Backend.DB.Daos.Abstract.Single.Attendance;
using Backend.Dtos.Attendance.Input;
using Backend.Dtos.Attendance.Output;
using Backend.Entities.Attendance;
using Backend.Hubs;
using Backend.Logging;
using Backend.Options;
using Backend.Results.Attendance;
using Backend.Services.Abstract;
using Backend.Services.Abstract.Attendance;
using Backend.Transporters.Entities;

using Microsoft.Extensions.Options;

namespace Backend.Services.Concrete.Attendance;

public sealed class ScheduledClassService(IScheduledClassAttendanceDao scheduledClassAttendanceDao,
                                           ICourseManagementClient courseManagementClient,
                                           IAttendanceMarker attendanceMarker,
                                           IClaimContext claimContext,
                                           IOptions<AttendanceOptions> attendanceOptions,
                                           IAttendanceClassBuilder attendanceClassBuilder,
                                           IMapper mapper,
                                           ILogger<ScheduledClassService> logger) : IScheduledClassService
{
    private readonly AttendanceOptions _attendanceOptions = attendanceOptions.Value;

    public async Task<List<ScheduledAttendanceResponse>> GetScheduledAttendance(Guid classId, DateOnly currentDate)
    {
        Guid tenantId = claimContext.TenantId;
        List<ScheduledClassAttendance> attendanceList =
            await scheduledClassAttendanceDao.GetScheduledAttendanceAsync(tenantId, classId, currentDate);
        return mapper.Map<List<ScheduledAttendanceResponse>>(attendanceList);
    }

    public async Task<GetScheduledByStudentOutcome> GetScheduledAttendanceByStudentId(Guid studentId)
    {
        Guid tenantId = claimContext.TenantId;
        if (claimContext.IsStudentAccessingOtherStudent(studentId))
        {
            return new GetScheduledByStudentOutcome.Forbidden();
        }

        List<ScheduledClassAttendance> attendanceList =
            await scheduledClassAttendanceDao.GetScheduledAttendanceByStudentIdAsync(tenantId, studentId);
        return new GetScheduledByStudentOutcome.Found(
            mapper.Map<List<ScheduledAttendanceResponse>>(attendanceList));
    }

    public async Task<PageDto<ScheduledAttendanceResponse>> ListMyScheduledAttendanceAsync(int pageIndex)
    {
        Guid tenantId = claimContext.TenantId;
        Guid studentId = claimContext.UserId;

        return await AttendancePaging.BuildPageAsync<ScheduledClassAttendance, ScheduledAttendanceResponse>(
            pageIndex,
            _attendanceOptions.PageSize,
            () => scheduledClassAttendanceDao.CountByStudentForTenantAsync(tenantId, studentId),
            (offset, limit) => scheduledClassAttendanceDao.GetPageByStudentForTenantAsync(tenantId, studentId, offset, limit),
            mapper,
            attendanceClassBuilder);
    }

    public async Task<MarkAttendanceOutcome> MarkScheduledAttendance(ScheduledAttendanceDto attendanceRequest)
    {
        return await attendanceMarker.MarkAsync<ScheduledClassAttendance, ScheduledAttendanceResponse>(
            async markContext =>
            {
                DateOnly classDate = LocalDateForTenant(markContext.TenantTimezoneId);
                ClassExistenceMeta? classMetadata =
                    await courseManagementClient.FindScheduledClassAsync(attendanceRequest.ClassId, classDate);
                if (classMetadata is null)
                {
                    return null;
                }

                ScheduledClassAttendance attendance = attendanceClassBuilder.BuildScheduledAttendance(
                    markContext.TenantId,
                    markContext.StudentId,
                    markContext.StudentName,
                    classDate,
                    attendanceRequest,
                    classMetadata);
                return new AttendanceBuildResult<ScheduledClassAttendance>(attendance, classMetadata.MaxStudentLimit);
            },
            (attendance, transaction) => scheduledClassAttendanceDao.CountOtherStudentsForUpdateAsync(
                attendance.TenantId, attendance.ClassId, attendance.ClassDate, attendance.StudentId, transaction),
            (attendance, transaction) => scheduledClassAttendanceDao.TryMarkAttendanceAsync(attendance, transaction),
            attendance => AttendanceHub.ScheduledGroup(attendance.TenantId, attendance.ClassId, attendance.ClassDate));
    }

    private DateOnly LocalDateForTenant(string ianaTimezoneId)
    {
        try
        {
            TimeZoneInfo timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(ianaTimezoneId);
            DateTime nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezoneInfo);
            return DateOnly.FromDateTime(nowLocal);
        }
        catch (Exception timezoneException) when (timezoneException is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            LogEvents.TenantTimezoneUnusableFallback(logger, timezoneException, ianaTimezoneId, timezoneException.GetType().Name);
            return DateOnly.FromDateTime(DateTime.UtcNow);
        }
    }
}
