using Backend.Dtos.Users.Input;
using Backend.Results.Users;

namespace Backend.Services.Abstract.Users;

public interface IAuthenticationService
{
    Task<LoginOutcome> LoginAsync(LoginCredentialsDto request);
}
