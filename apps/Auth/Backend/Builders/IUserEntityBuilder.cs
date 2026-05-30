using Backend.Dtos.Users.Input;
using Backend.Entities.Tenants;
using Backend.Entities.Users;

namespace Backend.Builders;

public interface IUserEntityBuilder
{
    User BuildUser(ICredentialsPayload request, UserRole role);

    TenantDomain BuildTenantDomain(Guid userId, Guid tenantId);
}
