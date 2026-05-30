using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.Results.Tenants;
using Backend.Services.Abstract.Tenants;

namespace Backend.Services.Concrete.Tenants;

public class TenantService : ITenantService
{
    private readonly ITenantDao _tenantDao;
    private readonly IClaimContext _claimContext;

    public TenantService(ITenantDao tenantDao,
                         IClaimContext claimContext)
    {
        _tenantDao = tenantDao;
        _claimContext = claimContext;
    }

    public async Task<UpdateTenantTimezoneOutcome> UpdateTenantTimezone(Guid tenantId, string newTimezone)
    {
        if (_claimContext.TenantId != tenantId)
        {
            return new UpdateTenantTimezoneOutcome.Forbidden();
        }

        int affected = await _tenantDao.UpdateTimezoneAsync(tenantId, newTimezone);
        return affected > 0
            ? new UpdateTenantTimezoneOutcome.Updated()
            : new UpdateTenantTimezoneOutcome.NotFound();
    }
}
