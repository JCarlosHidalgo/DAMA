using Backend.Transporters.Entities;

namespace Backend.Security;

public interface IRefreshTokenGenerator
{
    IssuedRefreshToken Issue(Guid userId);

    string ComputeHash(string rawToken);
}
