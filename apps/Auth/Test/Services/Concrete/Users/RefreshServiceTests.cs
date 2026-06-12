using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.DB.Daos.Abstract.Single.Tokens;
using Backend.Dtos.Users.Input;
using Backend.Dtos.Users.Output;
using Backend.Entities.Tenants;
using Backend.Entities.Tokens;
using Backend.Entities.Users;
using Backend.Security;
using Backend.Services.Concrete.Users;
using Backend.Transporters.Entities;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Test.Services.Concrete.Users;

[TestFixture]
public class RefreshServiceTests
{
    private Mock<IRefreshTokenReadDao> _refreshTokenReadDao = null!;
    private Mock<IRefreshTokenWriteDao> _refreshTokenWriteDao = null!;
    private Mock<ITenantAllowedServicesDao> _tenantAllowedServicesDao = null!;
    private Mock<IAccessTokenGenerator> _accessTokenGenerator = null!;
    private Mock<IRefreshTokenGenerator> _refreshTokenGenerator = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;

    private RefreshService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _refreshTokenReadDao = new Mock<IRefreshTokenReadDao>(MockBehavior.Strict);
        _refreshTokenWriteDao = new Mock<IRefreshTokenWriteDao>(MockBehavior.Strict);
        _tenantAllowedServicesDao = new Mock<ITenantAllowedServicesDao>(MockBehavior.Strict);
        _accessTokenGenerator = new Mock<IAccessTokenGenerator>(MockBehavior.Strict);
        _refreshTokenGenerator = new Mock<IRefreshTokenGenerator>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(unit => unit.BeginAsync()).ReturnsAsync(_transactionScope.Object);

        _sut = new RefreshService(
            _refreshTokenReadDao.Object,
            _refreshTokenWriteDao.Object,
            _tenantAllowedServicesDao.Object,
            _accessTokenGenerator.Object,
            _refreshTokenGenerator.Object,
            _unitOfWork.Object,
            NullLogger<RefreshService>.Instance);
    }

    private static RefreshTokenWithOwner OwnerFor(RefreshToken token)
    {
        User user = new() { Id = token.UserId, UserName = "owner", PasswordHash = "hash", Role = UserRole.Student.Value };
        Tenant tenant = new() { Id = Guid.NewGuid(), Name = "Academia", Timezone = "America/La_Paz" };
        return new RefreshTokenWithOwner(token, new UserWithTenant(user, tenant));
    }

    [Test]
    public async Task RefreshAsync_WhenTokenUnknown_ReturnsNull()
    {
        RefreshTokenRequestDto request = new() { RefreshToken = "unknown" };
        _refreshTokenGenerator.Setup(generator => generator.ComputeHash("unknown")).Returns("hash");
        _refreshTokenReadDao.Setup(dao => dao.GetByHashAsync("hash")).ReturnsAsync((RefreshTokenWithOwner?)null);

        TokenResponseDto? result = await _sut.RefreshAsync(request);

        Assert.That(result, Is.Null);
        _unitOfWork.Verify(unit => unit.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task RefreshAsync_WhenTokenAlreadyRevoked_RevokesAllForUserAndReturnsNull()
    {
        RefreshTokenRequestDto request = new() { RefreshToken = "reused" };
        RefreshToken revoked = new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow.AddMinutes(-5),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _refreshTokenGenerator.Setup(generator => generator.ComputeHash("reused")).Returns("hash");
        _refreshTokenReadDao.Setup(dao => dao.GetByHashAsync("hash")).ReturnsAsync(OwnerFor(revoked));
        _refreshTokenWriteDao
            .Setup(dao => dao.RevokeAllForUserAsync(revoked.UserId, _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        TokenResponseDto? result = await _sut.RefreshAsync(request);

        Assert.That(result, Is.Null);
        _refreshTokenWriteDao.Verify(dao => dao.RevokeAllForUserAsync(revoked.UserId, _transactionScope.Object), Times.Once);
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task RefreshAsync_WhenTokenExpired_ReturnsNull()
    {
        RefreshTokenRequestDto request = new() { RefreshToken = "expired" };
        RefreshToken expired = new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            RevokedAt = null,
            CreatedAt = DateTime.UtcNow.AddDays(-31)
        };
        _refreshTokenGenerator.Setup(generator => generator.ComputeHash("expired")).Returns("hash");
        _refreshTokenReadDao.Setup(dao => dao.GetByHashAsync("hash")).ReturnsAsync(OwnerFor(expired));

        TokenResponseDto? result = await _sut.RefreshAsync(request);

        Assert.That(result, Is.Null);
        _unitOfWork.Verify(unit => unit.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task RefreshAsync_WhenTokenValid_RotatesAndReturnsNewPair()
    {
        RefreshTokenRequestDto request = new() { RefreshToken = "valid" };
        RefreshToken current = new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            ExpiresAt = DateTime.UtcNow.AddDays(10),
            RevokedAt = null,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        RefreshTokenWithOwner stored = OwnerFor(current);
        RefreshToken rotated = new() { Id = Guid.NewGuid(), UserId = current.UserId, TokenHash = "newhash" };
        IssuedRefreshToken issued = new("new.raw.token", rotated);

        _refreshTokenGenerator.Setup(generator => generator.ComputeHash("valid")).Returns("hash");
        _refreshTokenReadDao.Setup(dao => dao.GetByHashAsync("hash")).ReturnsAsync(stored);
        _refreshTokenGenerator.Setup(generator => generator.Issue(current.UserId)).Returns(issued);
        _refreshTokenWriteDao.Setup(dao => dao.RevokeAsync(current.Id, _transactionScope.Object)).Returns(Task.CompletedTask);
        _refreshTokenWriteDao.Setup(dao => dao.CreateAsync(rotated, _transactionScope.Object)).Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);
        _tenantAllowedServicesDao
            .Setup(dao => dao.ReadByTenantIdAsync(stored.Owner.Tenant.Id))
            .ReturnsAsync((TenantAllowedServices?)null);
        _accessTokenGenerator
            .Setup(generator => generator.Issue(
                stored.Owner.User, stored.Owner.Tenant, It.IsAny<TenantAllowedServices?>()))
            .Returns("new.jwt");

        TokenResponseDto? result = await _sut.RefreshAsync(request);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.AccessToken, Is.EqualTo("new.jwt"));
            Assert.That(result.RefreshToken, Is.EqualTo("new.raw.token"));
        });
        _refreshTokenWriteDao.Verify(dao => dao.RevokeAsync(current.Id, _transactionScope.Object), Times.Once);
        _refreshTokenWriteDao.Verify(dao => dao.CreateAsync(rotated, _transactionScope.Object), Times.Once);
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task LogoutAsync_RevokesAllForUser()
    {
        Guid userId = Guid.NewGuid();
        _refreshTokenWriteDao
            .Setup(dao => dao.RevokeAllForUserAsync(userId, _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        await _sut.LogoutAsync(userId);

        _refreshTokenWriteDao.Verify(dao => dao.RevokeAllForUserAsync(userId, _transactionScope.Object), Times.Once);
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}
