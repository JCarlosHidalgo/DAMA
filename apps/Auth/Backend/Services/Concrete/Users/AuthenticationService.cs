using Backend.DB.Daos.Abstract.Single.Tokens;
using Backend.DB.Daos.Abstract.Single.Users;
using Backend.Dtos.Users.Input;
using Backend.Dtos.Users.Output;
using Backend.Entities.Users;
using Backend.Security;
using Backend.Services.Abstract.Users;
using Backend.Transporters.Entities;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.AspNetCore.Identity;

namespace Backend.Services.Concrete.Users;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserAuthenticationDao _userDao;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IAccessTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenWriteDao _refreshTokenWriteDao;
    private readonly IUnitOfWork _unitOfWork;

    public AuthenticationService(IUserAuthenticationDao userDao,
                                 IPasswordHasher<User> passwordHasher,
                                 IAccessTokenGenerator tokenGenerator,
                                 IRefreshTokenGenerator refreshTokenGenerator,
                                 IRefreshTokenWriteDao refreshTokenWriteDao,
                                 IUnitOfWork unitOfWork)
    {
        _userDao = userDao;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenWriteDao = refreshTokenWriteDao;
        _unitOfWork = unitOfWork;
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

        string accessToken = _tokenGenerator.Issue(user, userWithTenant.Tenant);
        IssuedRefreshToken issued = _refreshTokenGenerator.Issue(user.Id);

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();
        await _refreshTokenWriteDao.CreateAsync(issued.Entity, scope);
        await scope.CommitAsync();

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = issued.RawToken
        };
    }
}
