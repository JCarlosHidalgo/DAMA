using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Scheduleds;

public class ScheduledClass : IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [SmallInteger]
    public int DayOfWeekIndex { get; set; }

    [SmallInteger]
    public int MaxStudentLimit { get; set; }

    [Time]
    public TimeOnly StartTime { get; set; }

    [Time]
    public TimeOnly EndTime { get; set; }

    [Identifier]
    public Guid CourseId { get; set; }

    [Identifier]
    public Guid GroupId { get; set; }

    [Identifier]
    public Guid TenantId { get; set; }

    [NotPersisted]
    public string GroupName { get; set; } = string.Empty;

    [NotPersisted]
    public List<ClassTeacher> Teachers { get; set; } = new();
}
