namespace Backend.Dtos.Attendance.Input;

public class ScheduledAttendanceDto
{
    public required Guid ClassId { get; set; }

    public required string CourseName { get; set; }
}
