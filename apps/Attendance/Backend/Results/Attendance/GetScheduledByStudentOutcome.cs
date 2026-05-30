using Backend.Dtos.Attendance.Output;

namespace Backend.Results.Attendance;

public abstract record GetScheduledByStudentOutcome
{
    private GetScheduledByStudentOutcome() { }

    public sealed record Found(List<ScheduledAttendanceResponse> Attendances) : GetScheduledByStudentOutcome;

    public sealed record Forbidden : GetScheduledByStudentOutcome;
}
