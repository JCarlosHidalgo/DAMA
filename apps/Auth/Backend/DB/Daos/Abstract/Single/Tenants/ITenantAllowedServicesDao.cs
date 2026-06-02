using Backend.Entities.Tenants;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single.Tenants;

public interface ITenantAllowedServicesDao
{
    Task<TenantAllowedServices?> ReadByTenantIdAsync(Guid tenantId);

    Task UpsertAsync(TenantAllowedServices allowedServices, ITransactionContext transaction);

    Task<int> ResetExpiredAsync(DateTime asOf);
}
