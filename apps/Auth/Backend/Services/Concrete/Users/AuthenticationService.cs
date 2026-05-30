using Backend.DB.Daos.Abstract.Single.Users;
using Backend.Dtos.Users.Input;
using Backend.Dtos.Users.Output;
using Backend.Entities.Users;
using Backend.Security;
using Backend.Services.Abstract.Users;
using Backend.Transporters.Entities;

using Microsoft.AspNetCore.Identity;

namespace Backend.Services.Concrete.Users;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserAuthenticationDao _userDao;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IAccessTokenGenerator _tokenGenerator;

    public AuthenticationService(IUserAuthenticationDao userDao,
                                 IPasswordHasher<User> passwordHasher,
                                 IAccessTokenGenerator tokenGenerator)
    {
        _userDao = userDao;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<TokenResponseDto?> LoginAsync(LoginCredentialsDto request)
    {
        UserWithTenant? userWithTenant = await _userDao.ReadUserWithTenantByUserNameAsync(request.Username);
        if (userWithTenant is null)
        {
            return null;
        }

        User user = userWithTenant.User;

        if (
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password
            ) == PasswordVerificationResult.Failed
        )
        {
            return null;
        }

        return _tokenGenerator.Issue(user, userWithTenant.Tenant);
    }
}
