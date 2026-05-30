namespace Backend.Entities.Uniques;

public class UniqueClassAttendanceControl
{
    public Guid ClassId { get; set; }

    public Guid StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;
}
