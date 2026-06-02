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

using Moq;

namespace Test.Services.Concrete.Users;

[TestFixture]
public class RefreshServiceTests
{
    private Mock<IRefreshTokenReadDao> refreshTokenReadDao = null!;
    private Mock<IRefreshTokenWriteDao> refreshTokenWriteDao = null!;
    private Mock<ITenantAllowedServicesDao> tenantAllowedServicesDao = null!;
    private Mock<IAccessTokenGenerator> accessTokenGenerator = null!;
    private Mock<IRefreshTokenGenerator> refreshTokenGenerator = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;

    private RefreshService sut = null!;

    [SetUp]
    public void SetUp()
    {
        refreshTokenReadDao = new Mock<IRefreshTokenReadDao>(MockBehavior.Strict);
        refreshTokenWriteDao = new Mock<IRefreshTokenWriteDao>(MockBehavior.Strict);
        tenantAllowedServicesDao = new Mock<ITenantAllowedServicesDao>(MockBehavior.Strict);
        accessTokenGenerator = new Mock<IAccessTokenGenerator>(MockBehavior.Strict);
        refreshTokenGenerator = new Mock<IRefreshTokenGenerator>(MockBehavior.Strict);
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        unitOfWork.Setup(unit => unit.BeginAsync()).ReturnsAsync(transactionScope.Object);

        sut = new RefreshService(
            refreshTokenReadDao.Object,
            refreshTokenWriteDao.Object,
            tenantAllowedServicesDao.Object,
            accessTokenGenerator.Object,
            refreshTokenGenerator.Object,
            unitOfWork.Object);
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
        refreshTokenGenerator.Setup(generator => generator.ComputeHash("unknown")).Returns("hash");
        refreshTokenReadDao.Setup(dao => dao.GetByHashAsync("hash")).ReturnsAsync((RefreshTokenWithOwner?)null);

        TokenResponseDto? result = await sut.RefreshAsync(request);

        Assert.That(result, Is.Null);
        unitOfWork.Verify(unit => unit.BeginAsync(), Times.Never);
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
        refreshTokenGenerator.Setup(generator => generator.ComputeHash("reused")).Returns("hash");
        refreshTokenReadDao.Setup(dao => dao.GetByHashAsync("hash")).ReturnsAsync(OwnerFor(revoked));
        refreshTokenWriteDao
            .Setup(dao => dao.RevokeAllForUserAsync(revoked.UserId, transactionScope.Object))
            .Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        TokenResponseDto? result = await sut.RefreshAsync(request);

        Assert.That(result, Is.Null);
        refreshTokenWriteDao.Verify(dao => dao.RevokeAllForUserAsync(revoked.UserId, transactionScope.Object), Times.Once);
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
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
        refreshTokenGenerator.Setup(generator => generator.ComputeHash("expired")).Returns("hash");
        refreshTokenReadDao.Setup(dao => dao.GetByHashAsync("hash")).ReturnsAsync(OwnerFor(expired));

        TokenResponseDto? result = await sut.RefreshAsync(request);

        Assert.That(result, Is.Null);
        unitOfWork.Verify(unit => unit.BeginAsync(), Times.Never);
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

        refreshTokenGenerator.Setup(generator => generator.ComputeHash("valid")).Returns("hash");
        refreshTokenReadDao.Setup(dao => dao.GetByHashAsync("hash")).ReturnsAsync(stored);
        refreshTokenGenerator.Setup(generator => generator.Issue(current.UserId)).Returns(issued);
        refreshTokenWriteDao.Setup(dao => dao.RevokeAsync(current.Id, transactionScope.Object)).Returns(Task.CompletedTask);
        refreshTokenWriteDao.Setup(dao => dao.CreateAsync(rotated, transactionScope.Object)).Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);
        tenantAllowedServicesDao
            .Setup(dao => dao.ReadByTenantIdAsync(stored.Owner.Tenant.Id))
            .ReturnsAsync((TenantAllowedServices?)null);
        accessTokenGenerator
            .Setup(generator => generator.Issue(
                stored.Owner.User, stored.Owner.Tenant, It.IsAny<TenantAllowedServices?>()))
            .Returns("new.jwt");

        TokenResponseDto? result = await sut.RefreshAsync(request);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.AccessToken, Is.EqualTo("new.jwt"));
            Assert.That(result.RefreshToken, Is.EqualTo("new.raw.token"));
        });
        refreshTokenWriteDao.Verify(dao => dao.RevokeAsync(current.Id, transactionScope.Object), Times.Once);
        refreshTokenWriteDao.Verify(dao => dao.CreateAsync(rotated, transactionScope.Object), Times.Once);
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task LogoutAsync_RevokesAllForUser()
    {
        Guid userId = Guid.NewGuid();
        refreshTokenWriteDao
            .Setup(dao => dao.RevokeAllForUserAsync(userId, transactionScope.Object))
            .Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        await sut.LogoutAsync(userId);

        refreshTokenWriteDao.Verify(dao => dao.RevokeAllForUserAsync(userId, transactionScope.Object), Times.Once);
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
    }
}
