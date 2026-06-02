using Backend.Entities.Tokens;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single.Tokens;

public interface IRefreshTokenWriteDao
{
    Task CreateAsync(RefreshToken refreshToken, ITransactionContext transaction);

    Task RevokeAsync(Guid id, ITransactionContext transaction);

    Task RevokeAllForUserAsync(Guid userId, ITransactionContext transaction);
}
