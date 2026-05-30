using Backend.Dtos.Users.Input;
using Backend.Entities.Users;
using Backend.Results.Users;

namespace Backend.Services.Abstract.Users;

public interface IUserRegistrationService
{
    Task<RegisterUserOutcome> RegisterAsync(RegisterCredentialsDto request, UserRole role);
}
