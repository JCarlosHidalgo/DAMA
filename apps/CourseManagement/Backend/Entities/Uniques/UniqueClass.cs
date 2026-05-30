using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Uniques;

public class UniqueClass : IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Date]
    public DateOnly Date { get; set; }

    [SmallInteger]
    public int MaxStudentLimit { get; set; }

    [Time]
    public TimeOnly StartTime { get; set; }

    [Time]
    public TimeOnly EndTime { get; set; }

    [Identifier]
    public Guid CourseId { get; set; }

    [Identifier]
    public Guid TenantId { get; set; }

    [NotPersisted]
    public List<ClassTeacher> Teachers { get; set; } = new();
}
