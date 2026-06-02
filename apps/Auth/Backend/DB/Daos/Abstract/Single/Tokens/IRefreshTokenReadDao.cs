using Backend.Transporters.Entities;

namespace Backend.DB.Daos.Abstract.Single.Tokens;

public interface IRefreshTokenReadDao
{
    Task<RefreshTokenWithOwner?> GetByHashAsync(string tokenHash);
}
