using Backend.Entities.Tenants;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.Tenants;

public interface ITenantDao : ISingleDao<Tenant>
{
    Task<int> UpdateTimezoneAsync(Guid tenantId, string newTimezone);
}
