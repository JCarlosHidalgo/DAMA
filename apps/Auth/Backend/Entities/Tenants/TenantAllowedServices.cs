using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Tenants;

public class TenantAllowedServices : IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Integer]
    public int IndexCoreServicesPyramid { get; set; }

    [PreciseTimestamp]
    public DateTime ExpiresAt { get; set; }
}
