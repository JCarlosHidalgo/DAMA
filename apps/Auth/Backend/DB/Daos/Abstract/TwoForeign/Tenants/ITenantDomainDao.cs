using Backend.Entities.Tenants;

using DAMA.Software.MySqlUnitOfWork;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.TwoForeign.Tenants;

public interface ITenantDomainDao : ITwoForeignDao<TenantDomain>
{
    Task CreateAsync(TenantDomain domain, ITransactionContext transaction);
}
