using Backend.Dtos.Users.Input;
using Backend.Dtos.Users.Output;

namespace Backend.Services.Abstract.Users;

public interface IRefreshService
{
    Task<TokenResponseDto?> RefreshAsync(RefreshTokenRequestDto request);

    Task LogoutAsync(Guid userId);
}
