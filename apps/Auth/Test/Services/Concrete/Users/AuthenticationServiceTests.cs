using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.DB.Daos.Abstract.Single.Tokens;
using Backend.DB.Daos.Abstract.Single.Users;
using Backend.Dtos.Users.Input;
using Backend.Entities.Tenants;
using Backend.Entities.Tokens;
using Backend.Entities.Users;
using Backend.Results.Users;
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
    private Mock<IUserAuthenticationDao> _userAuthenticationDao = null!;
    private Mock<ITenantAllowedServicesDao> _tenantAllowedServicesDao = null!;
    private Mock<IPasswordHasher<User>> _passwordHasher = null!;
    private Mock<IAccessTokenGenerator> _accessTokenGenerator = null!;
    private Mock<IRefreshTokenGenerator> _refreshTokenGenerator = null!;
    private Mock<IRefreshTokenWriteDao> _refreshTokenWriteDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;

    private AuthenticationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _userAuthenticationDao = new Mock<IUserAuthenticationDao>(MockBehavior.Strict);
        _tenantAllowedServicesDao = new Mock<ITenantAllowedServicesDao>(MockBehavior.Strict);
        _passwordHasher = new Mock<IPasswordHasher<User>>(MockBehavior.Strict);
        _accessTokenGenerator = new Mock<IAccessTokenGenerator>(MockBehavior.Strict);
        _refreshTokenGenerator = new Mock<IRefreshTokenGenerator>(MockBehavior.Strict);
        _refreshTokenWriteDao = new Mock<IRefreshTokenWriteDao>(MockBehavior.Strict);
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(unit => unit.BeginAsync()).ReturnsAsync(_transactionScope.Object);

        _sut = new AuthenticationService(
            _userAuthenticationDao.Object,
            _tenantAllowedServicesDao.Object,
            _passwordHasher.Object,
            _accessTokenGenerator.Object,
            _refreshTokenGenerator.Object,
            _refreshTokenWriteDao.Object,
            _unitOfWork.Object,
            NullLogger<AuthenticationService>.Instance);
    }

    private static User BuildUser(string userName, DateTime? lockedUntil = null) => new()
    {
        Id = Guid.NewGuid(),
        UserName = userName,
        PasswordHash = "stored_hash",
        Role = UserRole.Student.Value,
        LockedUntil = lockedUntil
    };

    private static Tenant BuildTenant() =>
        new() { Id = Guid.NewGuid(), Name = "TenantOne", Timezone = "America/La_Paz" };

    [Test]
    public async Task LoginAsync_WhenUserNotFound_ReturnsInvalidCredentials()
    {
        LoginCredentialsDto request = new() { Username = "missing_user", Password = "any_pass" };
        _userAuthenticationDao
            .Setup(dao => dao.ReadUserWithTenantByUserNameAsync(request.Username))
            .ReturnsAsync((UserWithTenant?)null);

        LoginOutcome outcome = await _sut.LoginAsync(request);

        Assert.That(outcome, Is.TypeOf<LoginOutcome.InvalidCredentials>());
        _passwordHasher.Verify(
            hasher => hasher.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task LoginAsync_WhenAccountLocked_ReturnsAccountLockedWithoutVerifying()
    {
        LoginCredentialsDto request = new() { Username = "locked_user", Password = "any_pass" };
        User user = BuildUser(request.Username, lockedUntil: DateTime.UtcNow.AddMinutes(10));
        _userAuthenticationDao
            .Setup(dao => dao.ReadUserWithTenantByUserNameAsync(request.Username))
            .ReturnsAsync(new UserWithTenant(user, BuildTenant()));

        LoginOutcome outcome = await _sut.LoginAsync(request);

        Assert.That(outcome, Is.TypeOf<LoginOutcome.AccountLocked>());
        _passwordHasher.Verify(
            hasher => hasher.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task LoginAsync_WhenLockHasExpired_ProceedsToVerifyPassword()
    {
        LoginCredentialsDto request = new() { Username = "known_user", Password = "wrong_pass" };
        User user = BuildUser(request.Username, lockedUntil: DateTime.UtcNow.AddMinutes(-1));
        _userAuthenticationDao
            .Setup(dao => dao.ReadUserWithTenantByUserNameAsync(request.Username))
            .ReturnsAsync(new UserWithTenant(user, BuildTenant()));
        _passwordHasher
            .Setup(hasher => hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password))
            .Returns(PasswordVerificationResult.Failed);
        _userAuthenticationDao
            .Setup(dao => dao.RegisterFailedLoginAttemptAsync(user.Id))
            .Returns(Task.CompletedTask);

        LoginOutcome outcome = await _sut.LoginAsync(request);

        Assert.That(outcome, Is.TypeOf<LoginOutcome.InvalidCredentials>());
        _passwordHasher.Verify(
            hasher => hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password), Times.Once);
    }

    [Test]
    public async Task LoginAsync_WhenPasswordVerificationFails_RegistersFailedAttemptAndReturnsInvalidCredentials()
    {
        LoginCredentialsDto request = new() { Username = "known_user", Password = "wrong_pass" };
        User user = BuildUser(request.Username);
        _userAuthenticationDao
            .Setup(dao => dao.ReadUserWithTenantByUserNameAsync(request.Username))
            .ReturnsAsync(new UserWithTenant(user, BuildTenant()));
        _passwordHasher
            .Setup(hasher => hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password))
            .Returns(PasswordVerificationResult.Failed);
        _userAuthenticationDao
            .Setup(dao => dao.RegisterFailedLoginAttemptAsync(user.Id))
            .Returns(Task.CompletedTask);

        LoginOutcome outcome = await _sut.LoginAsync(request);

        Assert.That(outcome, Is.TypeOf<LoginOutcome.InvalidCredentials>());
        _userAuthenticationDao.Verify(dao => dao.RegisterFailedLoginAttemptAsync(user.Id), Times.Once);
        _accessTokenGenerator.Verify(
            generator => generator.Issue(
                It.IsAny<User>(), It.IsAny<Tenant>(), It.IsAny<TenantAllowedServices?>()),
            Times.Never);
    }

    [Test]
    public async Task LoginAsync_WhenCredentialsValid_ResetsFailuresAndIssuesTokenPair()
    {
        LoginCredentialsDto request = new() { Username = "known_user", Password = "correct_pass" };
        User user = BuildUser(request.Username);
        Tenant tenant = BuildTenant();
        RefreshToken refreshEntity = new() { Id = Guid.NewGuid(), UserId = user.Id, TokenHash = "hash" };
        IssuedRefreshToken issued = new("raw.refresh.token", refreshEntity);

        _userAuthenticationDao
            .Setup(dao => dao.ReadUserWithTenantByUserNameAsync(request.Username))
            .ReturnsAsync(new UserWithTenant(user, tenant));
        _passwordHasher
            .Setup(hasher => hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password))
            .Returns(PasswordVerificationResult.Success);
        _tenantAllowedServicesDao
            .Setup(dao => dao.ReadByTenantIdAsync(tenant.Id))
            .ReturnsAsync((TenantAllowedServices?)null);
        _accessTokenGenerator
            .Setup(generator => generator.Issue(user, tenant, It.IsAny<TenantAllowedServices?>()))
            .Returns("issued.jwt.token");
        _refreshTokenGenerator
            .Setup(generator => generator.Issue(user.Id))
            .Returns(issued);
        _userAuthenticationDao
            .Setup(dao => dao.ResetFailedLoginAttemptsAsync(user.Id, _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _refreshTokenWriteDao
            .Setup(dao => dao.CreateAsync(refreshEntity, _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        LoginOutcome outcome = await _sut.LoginAsync(request);

        LoginOutcome.Success success = (LoginOutcome.Success)outcome;
        Assert.Multiple(() =>
        {
            Assert.That(success.Tokens.AccessToken, Is.EqualTo("issued.jwt.token"));
            Assert.That(success.Tokens.RefreshToken, Is.EqualTo("raw.refresh.token"));
        });
        _userAuthenticationDao.Verify(dao => dao.ResetFailedLoginAttemptsAsync(user.Id, _transactionScope.Object), Times.Once);
        _transactionScope.Verify(scope => scope.CommitAsync(), Times.Once);
        _passwordHasher.Verify(hasher => hasher.HashPassword(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task LoginAsync_WhenRehashNeeded_PersistsUpgradedHashAndIssuesTokenPair()
    {
        LoginCredentialsDto request = new() { Username = "known_user", Password = "correct_pass" };
        User user = BuildUser(request.Username);
        user.PasswordHash = "old_hash";
        Tenant tenant = BuildTenant();
        RefreshToken refreshEntity = new() { Id = Guid.NewGuid(), UserId = user.Id, TokenHash = "hash" };
        IssuedRefreshToken issued = new("raw.refresh.token", refreshEntity);

        _userAuthenticationDao
            .Setup(dao => dao.ReadUserWithTenantByUserNameAsync(request.Username))
            .ReturnsAsync(new UserWithTenant(user, tenant));
        _passwordHasher
            .Setup(hasher => hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password))
            .Returns(PasswordVerificationResult.SuccessRehashNeeded);
        _passwordHasher
            .Setup(hasher => hasher.HashPassword(user, request.Password))
            .Returns("upgraded_hash");
        _tenantAllowedServicesDao
            .Setup(dao => dao.ReadByTenantIdAsync(tenant.Id))
            .ReturnsAsync((TenantAllowedServices?)null);
        _accessTokenGenerator
            .Setup(generator => generator.Issue(user, tenant, It.IsAny<TenantAllowedServices?>()))
            .Returns("issued.jwt.token");
        _refreshTokenGenerator
            .Setup(generator => generator.Issue(user.Id))
            .Returns(issued);
        _userAuthenticationDao
            .Setup(dao => dao.UpdatePasswordHashAsync(user.Id, "upgraded_hash", _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _userAuthenticationDao
            .Setup(dao => dao.ResetFailedLoginAttemptsAsync(user.Id, _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _refreshTokenWriteDao
            .Setup(dao => dao.CreateAsync(refreshEntity, _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        LoginOutcome outcome = await _sut.LoginAsync(request);

        Assert.That(outcome, Is.TypeOf<LoginOutcome.Success>());
        _userAuthenticationDao.Verify(
            dao => dao.UpdatePasswordHashAsync(user.Id, "upgraded_hash", _transactionScope.Object), Times.Once);
    }
}
