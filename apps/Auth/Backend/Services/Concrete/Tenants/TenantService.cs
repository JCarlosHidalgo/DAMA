using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.Dtos.Tenants.Input;
using Backend.Dtos.Tenants.Output;
using Backend.Entities.Tenants;
using Backend.Logging;
using Backend.Results.Tenants;
using Backend.Services.Abstract.Tenants;

namespace Backend.Services.Concrete.Tenants;

public class TenantService : ITenantService
{
    private readonly ITenantDao _tenantDao;
    private readonly IClaimContext _claimContext;
    private readonly ITenantBuilder _tenantBuilder;
    private readonly ILogger<TenantService> _logger;

    public TenantService(ITenantDao tenantDao,
                         IClaimContext claimContext,
                         ITenantBuilder tenantBuilder,
                         ILogger<TenantService> logger)
    {
        _tenantDao = tenantDao;
        _claimContext = claimContext;
        _tenantBuilder = tenantBuilder;
        _logger = logger;
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
        LogEvents.TenantCreated(_logger, tenant.Id, _claimContext.UserId);
        return _tenantBuilder.BuildTenantDto(tenant);
    }

    public async Task<UpdateTenantNameOutcome> RenameTenant(Guid tenantId, string newName)
    {
        int affected = await _tenantDao.UpdateNameAsync(tenantId, newName);
        if (affected > 0)
        {
            LogEvents.TenantRenamed(_logger, tenantId, _claimContext.UserId);
            return new UpdateTenantNameOutcome.Updated();
        }
        return new UpdateTenantNameOutcome.NotFound();
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
