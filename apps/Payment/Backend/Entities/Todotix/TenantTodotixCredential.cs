using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Todotix;

public class TenantTodotixCredential : IEntity
{
    [Identificator]
    public Guid TenantId { get; set; }

    [Text(512)]
    public string EncryptedAppKey { get; set; } = string.Empty;

    [Timestamp]
    public DateTime CreatedAt { get; set; }

    [Timestamp]
    public DateTime UpdatedAt { get; set; }
}
