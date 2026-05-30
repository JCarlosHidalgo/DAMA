using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Tenants;

public class TenantDomain : ITwoForeignEntity
{
    [FirstForeignId]
    public Guid UserId { get; set; }

    [SecondForeignId]
    public Guid TenantId { get; set; }
}
