using Backend.Dtos.Tenants.Output;
using Backend.Entities.Tenants;

namespace Backend.Builders;

public class TenantBuilder : ITenantBuilder
{
    private const string DefaultTimezone = "America/La_Paz";

    public Tenant BuildTenant(string name)
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Timezone = DefaultTimezone
        };
    }

    public TenantDto BuildTenantDto(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Timezone = tenant.Timezone
        };
    }

    public List<TenantDto> BuildTenantDtos(IEnumerable<Tenant> tenants)
    {
        return tenants.Select(BuildTenantDto).ToList();
    }
}
