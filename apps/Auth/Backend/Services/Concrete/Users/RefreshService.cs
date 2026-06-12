using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.DB.Daos.Abstract.Single.Tokens;
using Backend.Dtos.Users.Input;
using Backend.Dtos.Users.Output;
using Backend.Entities.Tenants;
using Backend.Logging;
using Backend.Security;
using Backend.Services.Abstract.Users;
using Backend.Transporters.Entities;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.Users;

public class RefreshService : IRefreshService
{
    private readonly IRefreshTokenReadDao _refreshTokenReadDao;
    private readonly IRefreshTokenWriteDao _refreshTokenWriteDao;
    private readonly ITenantAllowedServicesDao _tenantAllowedServicesDao;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshService> _logger;

    public RefreshService(IRefreshTokenReadDao refreshTokenReadDao,
                          IRefreshTokenWriteDao refreshTokenWriteDao,
                          ITenantAllowedServicesDao tenantAllowedServicesDao,
                          IAccessTokenGenerator accessTokenGenerator,
                          IRefreshTokenGenerator refreshTokenGenerator,
                          IUnitOfWork unitOfWork,
                          ILogger<RefreshService> logger)
    {
        _refreshTokenReadDao = refreshTokenReadDao;
        _refreshTokenWriteDao = refreshTokenWriteDao;
        _tenantAllowedServicesDao = tenantAllowedServicesDao;
        _accessTokenGenerator = accessTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TokenResponseDto?> RefreshAsync(RefreshTokenRequestDto request)
    {
        string tokenHash = _refreshTokenGenerator.ComputeHash(request.RefreshToken);
        RefreshTokenWithOwner? stored = await _refreshTokenReadDao.GetByHashAsync(tokenHash);
        if (stored is null)
        {
            return null;
        }

        if (stored.Token.RevokedAt is not null)
        {
            await using ITransactionScope reuseScope = await _unitOfWork.BeginAsync();
            await _refreshTokenWriteDao.RevokeAllForUserAsync(stored.Token.UserId, reuseScope);
            await reuseScope.CommitAsync();
            LogEvents.RefreshTokenReuseDetected(_logger, stored.Token.UserId);
            return null;
        }

        if (stored.Token.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        IssuedRefreshToken issued = _refreshTokenGenerator.Issue(stored.Token.UserId);

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();
        await _refreshTokenWriteDao.RevokeAsync(stored.Token.Id, scope);
        await _refreshTokenWriteDao.CreateAsync(issued.Entity, scope);
        await scope.CommitAsync();

        TenantAllowedServices? allowedServices =
            await _tenantAllowedServicesDao.ReadByTenantIdAsync(stored.Owner.Tenant.Id);

        LogEvents.TokenRefreshed(_logger, stored.Token.UserId);

        return new TokenResponseDto
        {
            AccessToken = _accessTokenGenerator.Issue(stored.Owner.User, stored.Owner.Tenant, allowedServices),
            RefreshToken = issued.RawToken
        };
    }

    public async Task LogoutAsync(Guid userId)
    {
        await using ITransactionScope scope = await _unitOfWork.BeginAsync();
        await _refreshTokenWriteDao.RevokeAllForUserAsync(userId, scope);
        await scope.CommitAsync();
        LogEvents.UserLoggedOut(_logger, userId);
    }
}
