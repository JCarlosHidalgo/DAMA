using Backend.Entities.Users;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single.Users;

public interface IUserRegistrationDao
{
    Task<bool> TryCreateAsync(User user, ITransactionContext transaction);
}
