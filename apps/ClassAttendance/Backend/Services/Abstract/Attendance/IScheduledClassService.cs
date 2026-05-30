using Backend.Common;
using Backend.Dtos.Attendance.Input;
using Backend.Dtos.Attendance.Output;
using Backend.Results.Attendance;

namespace Backend.Services.Abstract.Attendance;

public interface IScheduledClassService
{
    Task<List<ScheduledAttendanceResponse>> GetScheduledAttendance(Guid classId, DateOnly currentDate);

    Task<GetScheduledByStudentOutcome> GetScheduledAttendanceByStudentId(Guid studentId);

    Task<PageDto<ScheduledAttendanceResponse>> ListMyScheduledAttendanceAsync(int pageIndex);

    Task<MarkAttendanceOutcome> MarkScheduledAttendance(ScheduledAttendanceDto attendanceRequest);
}
