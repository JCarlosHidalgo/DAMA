using Backend.Dtos.Attendance.Output;

namespace Backend.Results.Attendance;

public abstract record GetUniqueByStudentOutcome
{
    private GetUniqueByStudentOutcome() { }

    public sealed record Found(List<UniqueAttendanceResponse> Attendances) : GetUniqueByStudentOutcome;

    public sealed record Forbidden : GetUniqueByStudentOutcome;
}
