using Backend.Entities.Tenants;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.Tenants;

public interface ITenantDao : ISingleDao<Tenant>
{
    Task<List<Tenant>> ReadAllAsync();

    Task CreateTenantAsync(Tenant tenant);

    Task<int> UpdateNameAsync(Guid tenantId, string newName);

    Task<int> UpdateTimezoneAsync(Guid tenantId, string newTimezone);
}
