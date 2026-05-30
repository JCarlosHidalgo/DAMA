using Backend.Transporters.Entities;

namespace Backend.DB.Daos.Abstract.Single.Users;

public interface IUserAuthenticationDao
{
    Task<UserWithTenant?> ReadUserWithTenantByUserNameAsync(string userName);
}
