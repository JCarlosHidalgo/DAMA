using Backend.Entities.DebtTemplates;

using DAMA.Software.MySqlUnitOfWork;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.DebtTemplates;

public interface IDebtTemplateDao : ISingleDao<DebtTemplate>
{
    new Task CreateAsync(DebtTemplate template);

    Task CreateAsync(DebtTemplate template, ITransactionContext transaction);

    Task<List<DebtTemplate>> GetByTenantAsync(Guid tenantId);

    Task<DebtTemplate?> GetByIdForTenantAsync(Guid tenantId, Guid templateId);

    Task<bool> UpdateForTenantAsync(Guid tenantId, Guid templateId, string description, int classQuantity, int cost);

    Task<bool> DeleteForTenantAsync(Guid tenantId, Guid templateId);
}
