using Backend.Dtos.Users.Output;
using Backend.Entities.Tenants;
using Backend.Entities.Users;

namespace Backend.Security;

public interface IAccessTokenGenerator
{
    TokenResponseDto Issue(User user, Tenant tenant);
}
