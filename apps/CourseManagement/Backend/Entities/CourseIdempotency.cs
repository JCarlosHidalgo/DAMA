using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities;

public class CourseIdempotency
{
    [Identifier]
    public Guid TenantId { get; set; }

    [Text(128)]
    public string ExternalReference { get; set; } = string.Empty;

    [Text(32)]
    public string EntityType { get; set; } = string.Empty;

    [Identifier]
    public Guid EntityId { get; set; }

    [Timestamp]
    public DateTime ProcessedAt { get; set; }
}
