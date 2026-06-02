using Backend.Entities.Tenants;
using Backend.Entities.Users;

namespace Backend.Security;

public interface IAccessTokenGenerator
{
    string Issue(User user, Tenant tenant, TenantAllowedServices? allowedServices);
}
