using Backend.Results.Tenants;

namespace Backend.Services.Abstract.Tenants;

public interface ITenantService
{
    Task<UpdateTenantTimezoneOutcome> UpdateTenantTimezone(Guid tenantId, string newTimezone);
}
