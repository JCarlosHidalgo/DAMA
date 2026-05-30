using AutoMapper;

using Backend.Builders;
using Backend.Claims;
using Backend.Common;
using Backend.DB.Daos.Abstract.Single.Attendance;
using Backend.Dtos.Attendance.Input;
using Backend.Dtos.Attendance.Output;
using Backend.Entities.Attendance;
using Backend.Hubs;
using Backend.Options;
using Backend.Results.Attendance;
using Backend.Services.Abstract;
using Backend.Services.Abstract.Attendance;
using Backend.Transporters.Entities;

using Microsoft.Extensions.Options;

namespace Backend.Services.Concrete.Attendance;

public sealed class UniqueClassService(IUniqueClassAttendanceDao uniqueClassAttendanceDao,
                                        ICourseManagementClient courseManagementClient,
                                        IAttendanceMarker attendanceMarker,
                                        IClaimContext claimContext,
                                        IOptions<AttendanceOptions> attendanceOptions,
                                        IAttendanceClassBuilder attendanceClassBuilder,
                                        IMapper mapper) : IUniqueClassService
{
    private readonly AttendanceOptions _attendanceOptions = attendanceOptions.Value;

    public async Task<List<UniqueAttendanceResponse>> GetUniqueAttendance(Guid classId)
    {
        Guid tenantId = claimContext.TenantId;
        List<UniqueClassAttendance> attendanceList =
            await uniqueClassAttendanceDao.GetUniqueAttendanceAsync(tenantId, classId);
        return mapper.Map<List<UniqueAttendanceResponse>>(attendanceList);
    }

    public async Task<GetUniqueByStudentOutcome> GetUniqueAttendanceByStudentId(Guid studentId)
    {
        Guid tenantId = claimContext.TenantId;
        if (claimContext.IsStudentAccessingOtherStudent(studentId))
        {
            return new GetUniqueByStudentOutcome.Forbidden();
        }

        List<UniqueClassAttendance> attendanceList =
            await uniqueClassAttendanceDao.GetUniqueAttendanceByStudentIdAsync(tenantId, studentId);
        return new GetUniqueByStudentOutcome.Found(
            mapper.Map<List<UniqueAttendanceResponse>>(attendanceList));
    }

    public async Task<PageDto<UniqueAttendanceResponse>> ListMyUniqueAttendanceAsync(int pageIndex)
    {
        Guid tenantId = claimContext.TenantId;
        Guid studentId = claimContext.UserId;

        return await AttendancePaging.BuildPageAsync<UniqueClassAttendance, UniqueAttendanceResponse>(
            pageIndex,
            _attendanceOptions.PageSize,
            () => uniqueClassAttendanceDao.CountByStudentForTenantAsync(tenantId, studentId),
            (offset, limit) => uniqueClassAttendanceDao.GetPageByStudentForTenantAsync(tenantId, studentId, offset, limit),
            mapper,
            attendanceClassBuilder);
    }

    public async Task<MarkAttendanceOutcome> MarkUniqueAttendance(UniqueAttendanceDto attendanceRequest)
    {
        return await attendanceMarker.MarkAsync<UniqueClassAttendance, UniqueAttendanceResponse>(
            async markContext =>
            {
                ClassExistenceMeta? classMetadata =
                    await courseManagementClient.FindUniqueClassAsync(attendanceRequest.ClassId);
                if (classMetadata is null)
                {
                    return null;
                }

                DateOnly classDate = classMetadata.ClassDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

                UniqueClassAttendance attendance = attendanceClassBuilder.BuildUniqueAttendance(
                    markContext.TenantId,
                    markContext.StudentId,
                    markContext.StudentName,
                    classDate,
                    attendanceRequest,
                    classMetadata);
                return new AttendanceBuildResult<UniqueClassAttendance>(attendance, classMetadata.MaxStudentLimit);
            },
            (attendance, transaction) => uniqueClassAttendanceDao.CountOtherStudentsForUpdateAsync(
                attendance.TenantId, attendance.ClassId, attendance.StudentId, transaction),
            (attendance, transaction) => uniqueClassAttendanceDao.TryMarkAttendanceAsync(attendance, transaction),
            attendance => AttendanceHub.UniqueGroup(attendance.TenantId, attendance.ClassId));
    }
}
