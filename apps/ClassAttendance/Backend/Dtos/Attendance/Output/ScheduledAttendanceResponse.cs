namespace Backend.Dtos.Attendance.Output;

public sealed class ScheduledAttendanceResponse : IAttendanceLine
{
    public Guid ClassId { get; set; }

    public DateOnly ClassDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public Guid StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;
}
