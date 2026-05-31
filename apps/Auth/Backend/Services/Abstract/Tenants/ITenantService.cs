using Backend.Dtos.Tenants.Input;
using Backend.Dtos.Tenants.Output;
using Backend.Results.Tenants;

namespace Backend.Services.Abstract.Tenants;

public interface ITenantService
{
    Task<List<TenantDto>> GetAllTenants();

    Task<TenantDto> CreateTenant(CreateTenantDto request);

    Task<UpdateTenantNameOutcome> RenameTenant(Guid tenantId, string newName);

    Task<UpdateTenantTimezoneOutcome> UpdateTenantTimezone(Guid tenantId, string newTimezone);
}
