using Backend.Dtos.Users.Input;
using Backend.Dtos.Users.Output;

namespace Backend.Services.Abstract.Users;

public interface IAuthenticationService
{
    Task<TokenResponseDto?> LoginAsync(LoginCredentialsDto request);
}
