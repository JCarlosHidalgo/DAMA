using Backend.Transporters.Entities;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single.Users;

public interface IUserAuthenticationDao
{
    Task<UserWithTenant?> ReadUserWithTenantByUserNameAsync(string userName);

    Task RegisterFailedLoginAttemptAsync(Guid userId);

    Task ResetFailedLoginAttemptsAsync(Guid userId, ITransactionContext transaction);

    Task UpdatePasswordHashAsync(Guid userId, string passwordHash, ITransactionContext transaction);
}
