using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Attendance;

public class UniqueClassAttendance : IThreeForeignEntity
{
    [FirstForeignId]
    public Guid TenantId { get; set; }

    [SecondForeignId]
    public Guid ClassId { get; set; }

    [Date]
    public DateOnly ClassDate { get; set; }

    [Time]
    public TimeOnly StartTime { get; set; }

    [Time]
    public TimeOnly EndTime { get; set; }

    [Text(80)]
    public string CourseName { get; set; } = string.Empty;

    [ThirdForeignId]
    public Guid StudentId { get; set; }

    [Text(80)]
    public string StudentName { get; set; } = string.Empty;
}
