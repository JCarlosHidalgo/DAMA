namespace Backend.Dtos.Attendance.Input;

public class UniqueAttendanceDto
{
    public required Guid ClassId { get; set; }

    public required string CourseName { get; set; }
}
