using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.Entities.Tenants;
using Backend.Services.Abstract.Subscriptions;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.Subscriptions;

public sealed class TenantSubscriptionUpdater : ITenantSubscriptionUpdater
{
    private readonly ITenantAllowedServicesDao _tenantAllowedServicesDao;
    private readonly IUnitOfWork _unitOfWork;

    public TenantSubscriptionUpdater(ITenantAllowedServicesDao tenantAllowedServicesDao, IUnitOfWork unitOfWork)
    {
        _tenantAllowedServicesDao = tenantAllowedServicesDao;
        _unitOfWork = unitOfWork;
    }

    public async Task UpdateAsync(Guid tenantId, int level, DateTime newExpiresAtUtc)
    {
        TenantAllowedServices allowedServices = new TenantAllowedServices
        {
            Id = tenantId,
            IndexCoreServicesPyramid = level,
            ExpiresAt = newExpiresAtUtc
        };

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();
        await _tenantAllowedServicesDao.UpsertAsync(allowedServices, scope);
        await scope.CommitAsync();
    }
}
