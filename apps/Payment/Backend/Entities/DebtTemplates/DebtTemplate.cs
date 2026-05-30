using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.DebtTemplates;

public class DebtTemplate : IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Identifier]
    public Guid TenantId { get; set; }

    [Text(256)]
    public string Description { get; set; } = string.Empty;

    [Integer]
    public int ClassQuantity { get; set; }

    [Integer]
    public int Cost { get; set; }
}
