using Backend.Common;
using Backend.Dtos.Attendance.Input;
using Backend.Dtos.Attendance.Output;
using Backend.Results.Attendance;

namespace Backend.Services.Abstract.Attendance;

public interface IUniqueClassService
{
    Task<List<UniqueAttendanceResponse>> GetUniqueAttendance(Guid classId);

    Task<GetUniqueByStudentOutcome> GetUniqueAttendanceByStudentId(Guid studentId);

    Task<PageDto<UniqueAttendanceResponse>> ListMyUniqueAttendanceAsync(int pageIndex);

    Task<MarkAttendanceOutcome> MarkUniqueAttendance(UniqueAttendanceDto attendanceRequest);
}
