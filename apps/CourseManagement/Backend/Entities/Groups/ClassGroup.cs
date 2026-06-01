using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Groups;

public class ClassGroup : IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Text(200)]
    public string Name { get; set; } = string.Empty;

    [Identifier]
    public Guid TenantId { get; set; }
}
