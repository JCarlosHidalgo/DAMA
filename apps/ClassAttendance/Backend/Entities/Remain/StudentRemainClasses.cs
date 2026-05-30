using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Remain;

public class StudentRemainClasses : IEntity
{
    [Identifier]
    public Guid TenantId { get; set; }

    [Identificator]
    public Guid Id { get; set; }

    [Integer]
    public int NumberOfClasses { get; set; }

    [Text(80)]
    public string? StudentName { get; set; }
}
