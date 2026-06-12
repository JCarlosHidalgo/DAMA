using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.DB.Daos.Abstract.Single.Tokens;
using Backend.DB.Daos.Abstract.Single.Users;
using Backend.Dtos.Users.Input;
using Backend.Dtos.Users.Output;
using Backend.Entities.Tenants;
using Backend.Entities.Users;
using Backend.Logging;
using Backend.Results.Users;
using Backend.Security;
using Backend.Services.Abstract.Users;
using Backend.Transporters.Entities;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.AspNetCore.Identity;

namespace Backend.Services.Concrete.Users;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserAuthenticationDao _userDao;
    private readonly ITenantAllowedServicesDao _tenantAllowedServicesDao;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IAccessTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenWriteDao _refreshTokenWriteDao;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(IUserAuthenticationDao userDao,
                                 ITenantAllowedServicesDao tenantAllowedServicesDao,
                                 IPasswordHasher<User> passwordHasher,
                                 IAccessTokenGenerator tokenGenerator,
                                 IRefreshTokenGenerator refreshTokenGenerator,
                                 IRefreshTokenWriteDao refreshTokenWriteDao,
                                 IUnitOfWork unitOfWork,
                                 ILogger<AuthenticationService> logger)
    {
        _userDao = userDao;
        _tenantAllowedServicesDao = tenantAllowedServicesDao;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenWriteDao = refreshTokenWriteDao;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<LoginOutcome> LoginAsync(LoginCredentialsDto request)
    {
        UserWithTenant? userWithTenant = await _userDao.ReadUserWithTenantByUserNameAsync(request.Username);
        if (userWithTenant is null)
        {
            LogEvents.LoginFailedUserNotFound(_logger, request.Username);
            return new LoginOutcome.InvalidCredentials();
        }

        User user = userWithTenant.User;

        if (user.LockedUntil is DateTime lockedUntil && lockedUntil > DateTime.UtcNow)
        {
            LogEvents.LoginBlockedAccountLocked(_logger, user.Id);
            return new LoginOutcome.AccountLocked();
        }

        PasswordVerificationResult verification =
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            await _userDao.RegisterFailedLoginAttemptAsync(user.Id);
            LogEvents.LoginFailedInvalidPassword(_logger, request.Username);
            return new LoginOutcome.InvalidCredentials();
        }

        TenantAllowedServices? allowedServices =
            await _tenantAllowedServicesDao.ReadByTenantIdAsync(userWithTenant.Tenant.Id);

        string accessToken = _tokenGenerator.Issue(user, userWithTenant.Tenant, allowedServices);
        IssuedRefreshToken issued = _refreshTokenGenerator.Issue(user.Id);

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            string upgradedHash = _passwordHasher.HashPassword(user, request.Password);
            await _userDao.UpdatePasswordHashAsync(user.Id, upgradedHash, scope);
            LogEvents.PasswordHashUpgraded(_logger, user.Id);
        }
        await _userDao.ResetFailedLoginAttemptsAsync(user.Id, scope);
        await _refreshTokenWriteDao.CreateAsync(issued.Entity, scope);
        await scope.CommitAsync();

        LogEvents.LoginSucceeded(_logger, user.Id, userWithTenant.Tenant.Id);

        return new LoginOutcome.Success(new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = issued.RawToken
        });
    }
}
