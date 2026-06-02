using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.DB.Daos.Abstract.Single.Tokens;
using Backend.DB.Daos.Abstract.Single.Users;
using Backend.Dtos.Users.Input;
using Backend.Dtos.Users.Output;
using Backend.Entities.Tenants;
using Backend.Entities.Tokens;
using Backend.Entities.Users;
using Backend.Security;
using Backend.Services.Concrete.Users;
using Backend.Transporters.Entities;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Test.Services.Concrete.Users;

[TestFixture]
public class AuthenticationServiceTests
{
    private Mock<IUserAuthenticationDao> userAuthenticationDao = null!;
    private Mock<ITenantAllowedServicesDao> tenantAllowedServicesDao = null!;
    private Mock<IPasswordHasher<User>> passwordHasher = null!;
    private Mock<IAccessTokenGenerator> accessTokenGenerator = null!;
    private Mock<IRefreshTokenGenerator> refreshTokenGenerator = null!;
    private Mock<IRefreshTokenWriteDao> refreshTokenWriteDao = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;

    private AuthenticationService sut = null!;

    [SetUp]
    public void SetUp()
    {
        userAuthenticationDao = new Mock<IUserAuthenticationDao>(MockBehavior.Strict);
        tenantAllowedServicesDao = new Mock<ITenantAllowedServicesDao>(MockBehavior.Strict);
        passwordHasher = new Mock<IPasswordHasher<User>>(MockBehavior.Strict);
        accessTokenGenerator = new Mock<IAccessTokenGenerator>(MockBehavior.Strict);
        refreshTokenGenerator = new Mock<IRefreshTokenGenerator>(MockBehavior.Strict);
        refreshTokenWriteDao = new Mock<IRefreshTokenWriteDao>(MockBehavior.Strict);
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        unitOfWork.Setup(unit => unit.BeginAsync()).ReturnsAsync(transactionScope.Object);

        sut = new AuthenticationService(
            userAuthenticationDao.Object,
            tenantAllowedServicesDao.Object,
            passwordHasher.Object,
            accessTokenGenerator.Object,
            refreshTokenGenerator.Object,
            refreshTokenWriteDao.Object,
            unitOfWork.Object,
            NullLogger<AuthenticationService>.Instance);
    }

    [Test]
    public async Task LoginAsync_WhenUserNotFound_ReturnsNull()
    {
        LoginCredentialsDto request = new() { Username = "missing_user", Password = "any_pass" };
        userAuthenticationDao
            .Setup(dao => dao.ReadUserWithTenantByUserNameAsync(request.Username))
            .ReturnsAsync((UserWithTenant?)null);

        TokenResponseDto? token = await sut.LoginAsync(request);

        Assert.That(token, Is.Null);
        passwordHasher.Verify(
            hasher => hasher.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        accessTokenGenerator.Verify(
            generator => generator.Issue(
                It.IsAny<User>(), It.IsAny<Tenant>(), It.IsAny<TenantAllowedServices?>()),
            Times.Never);
    }

    [Test]
    public async Task LoginAsync_WhenPasswordVerificationFails_ReturnsNull()
    {
        LoginCredentialsDto request = new() { Username = "known_user", Password = "wrong_pass" };
        User user = new()
        {
            Id = Guid.NewGuid(),
            UserName = request.Username,
            PasswordHash = "stored_hash",
            Role = UserRole.Teacher.Value
        };
        Tenant tenant = new() { Id = Guid.NewGuid(), Name = "TenantOne", Timezone = "America/La_Paz" };
        UserWithTenant userWithTenant = new(user, tenant);

        userAuthenticationDao
            .Setup(dao => dao.ReadUserWithTenantByUserNameAsync(request.Username))
            .ReturnsAsync(userWithTenant);
        passwordHasher
            .Setup(hasher => hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password))
            .Returns(PasswordVerificationResult.Failed);

        TokenResponseDto? token = await sut.LoginAsync(request);

        Assert.That(token, Is.Null);
        accessTokenGenerator.Verify(
            generator => generator.Issue(
                It.IsAny<User>(), It.IsAny<Tenant>(), It.IsAny<TenantAllowedServices?>()),
            Times.Never);
    }

    [Test]
    public async Task LoginAsync_WhenCredentialsValid_IssuesAndPersistsTokenPair()
    {
        LoginCredentialsDto request = new() { Username = "known_user", Password = "correct_pass" };
        User user = new()
        {
            Id = Guid.NewGuid(),
            UserName = request.Username,
            PasswordHash = "stored_hash",
            Role = UserRole.Student.Value
        };
        Tenant tenant = new() { Id = Guid.NewGuid(), Name = "TenantOne", Timezone = "America/La_Paz" };
        UserWithTenant userWithTenant = new(user, tenant);
        RefreshToken refreshEntity = new() { Id = Guid.NewGuid(), UserId = user.Id, TokenHash = "hash" };
        IssuedRefreshToken issued = new("raw.refresh.token", refreshEntity);

        userAuthenticationDao
            .Setup(dao => dao.ReadUserWithTenantByUserNameAsync(request.Username))
            .ReturnsAsync(userWithTenant);
        passwordHasher
            .Setup(hasher => hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password))
            .Returns(PasswordVerificationResult.Success);
        tenantAllowedServicesDao
            .Setup(dao => dao.ReadByTenantIdAsync(tenant.Id))
            .ReturnsAsync((TenantAllowedServices?)null);
        accessTokenGenerator
            .Setup(generator => generator.Issue(user, tenant, It.IsAny<TenantAllowedServices?>()))
            .Returns("issued.jwt.token");
        refreshTokenGenerator
            .Setup(generator => generator.Issue(user.Id))
            .Returns(issued);
        refreshTokenWriteDao
            .Setup(dao => dao.CreateAsync(refreshEntity, transactionScope.Object))
            .Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        TokenResponseDto? token = await sut.LoginAsync(request);

        Assert.That(token, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(token!.AccessToken, Is.EqualTo("issued.jwt.token"));
            Assert.That(token.RefreshToken, Is.EqualTo("raw.refresh.token"));
        });
        refreshTokenWriteDao.Verify(dao => dao.CreateAsync(refreshEntity, transactionScope.Object), Times.Once);
        transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        transactionScope.Verify(scope => scope.DisposeAsync(), Times.Once);
    }

    [Test]
    public async Task LoginAsync_WhenPasswordVerificationSucceedsRehashNeeded_StillIssuesTokenPair()
    {
        LoginCredentialsDto request = new() { Username = "known_user", Password = "correct_pass" };
        User user = new() { Id = Guid.NewGuid(), UserName = request.Username, PasswordHash = "old_hash" };
        Tenant tenant = new() { Id = Guid.NewGuid(), Name = "TenantOne", Timezone = "America/La_Paz" };
        RefreshToken refreshEntity = new() { Id = Guid.NewGuid(), UserId = user.Id, TokenHash = "hash" };
        IssuedRefreshToken issued = new("raw.refresh.token", refreshEntity);

        userAuthenticationDao
            .Setup(dao => dao.ReadUserWithTenantByUserNameAsync(request.Username))
            .ReturnsAsync(new UserWithTenant(user, tenant));
        passwordHasher
            .Setup(hasher => hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password))
            .Returns(PasswordVerificationResult.SuccessRehashNeeded);
        tenantAllowedServicesDao
            .Setup(dao => dao.ReadByTenantIdAsync(tenant.Id))
            .ReturnsAsync((TenantAllowedServices?)null);
        accessTokenGenerator
            .Setup(generator => generator.Issue(user, tenant, It.IsAny<TenantAllowedServices?>()))
            .Returns("issued.jwt.token");
        refreshTokenGenerator
            .Setup(generator => generator.Issue(user.Id))
            .Returns(issued);
        refreshTokenWriteDao
            .Setup(dao => dao.CreateAsync(refreshEntity, transactionScope.Object))
            .Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        TokenResponseDto? token = await sut.LoginAsync(request);

        Assert.That(token, Is.Not.Null);
        Assert.That(token!.RefreshToken, Is.EqualTo("raw.refresh.token"));
    }
}
