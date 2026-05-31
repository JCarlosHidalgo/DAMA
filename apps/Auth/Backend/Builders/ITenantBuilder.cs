using Backend.Dtos.Tenants.Output;
using Backend.Entities.Tenants;

namespace Backend.Builders;

public interface ITenantBuilder
{
    Tenant BuildTenant(string name);

    TenantDto BuildTenantDto(Tenant tenant);

    List<TenantDto> BuildTenantDtos(IEnumerable<Tenant> tenants);
}
