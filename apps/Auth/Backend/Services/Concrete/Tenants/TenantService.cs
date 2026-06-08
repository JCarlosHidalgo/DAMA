using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.Dtos.Tenants.Input;
using Backend.Dtos.Tenants.Output;
using Backend.Entities.Tenants;
using Backend.Results.Tenants;
using Backend.Services.Abstract.Tenants;

namespace Backend.Services.Concrete.Tenants;

public class TenantService : ITenantService
{
    private readonly ITenantDao _tenantDao;
    private readonly IClaimContext _claimContext;
    private readonly ITenantBuilder _tenantBuilder;

    public TenantService(ITenantDao tenantDao,
                         IClaimContext claimContext,
                         ITenantBuilder tenantBuilder)
    {
        _tenantDao = tenantDao;
        _claimContext = claimContext;
        _tenantBuilder = tenantBuilder;
    }

    public async Task<List<TenantDto>> GetAllTenants()
    {
        List<Tenant> tenants = await _tenantDao.ReadAllAsync();
        return _tenantBuilder.BuildTenantDtos(tenants);
    }

    public async Task<TenantDto> CreateTenant(CreateTenantDto request)
    {
        Tenant tenant = _tenantBuilder.BuildTenant(request.Name);
        await _tenantDao.CreateTenantAsync(tenant);
        return _tenantBuilder.BuildTenantDto(tenant);
    }

    public async Task<UpdateTenantNameOutcome> RenameTenant(Guid tenantId, string newName)
    {
        int affected = await _tenantDao.UpdateNameAsync(tenantId, newName);
        return affected > 0
            ? new UpdateTenantNameOutcome.Updated()
            : new UpdateTenantNameOutcome.NotFound();
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

    public async Task<List<TenantTierCountDto>> GetTierDistribution()
    {
        List<TenantTierCountRow> rows = await _tenantDao.GetCountBySubscriptionTierAsync();

        return rows
            .Select(row => new TenantTierCountDto
            {
                Tier = row.Tier,
                TenantCount = row.TenantCount
            })
            .ToList();
    }
}
