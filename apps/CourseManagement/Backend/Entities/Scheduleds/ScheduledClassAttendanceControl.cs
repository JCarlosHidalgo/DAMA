namespace Backend.Entities.Scheduleds;

public class ScheduledClassAttendanceControl
{
    public Guid ClassId { get; set; }

    public DateOnly ClassDate { get; set; }

    public Guid StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;
}
