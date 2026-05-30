using Backend.DB.Daos.Abstract.Single.Users;
using Backend.Dtos.Users.Input;
using Backend.Dtos.Users.Output;
using Backend.Entities.Tenants;
using Backend.Entities.Users;
using Backend.Security;
using Backend.Services.Concrete.Users;
using Backend.Transporters.Entities;

using Microsoft.AspNetCore.Identity;

using Moq;

namespace Test.Services.Concrete.Users;

[TestFixture]
public class AuthenticationServiceTests
{
    private Mock<IUserAuthenticationDao> userAuthenticationDao = null!;
    private Mock<IPasswordHasher<User>> passwordHasher = null!;
    private Mock<IAccessTokenGenerator> accessTokenGenerator = null!;

    private AuthenticationService sut = null!;

    [SetUp]
    public void SetUp()
    {
        userAuthenticationDao = new Mock<IUserAuthenticationDao>(MockBehavior.Strict);
        passwordHasher = new Mock<IPasswordHasher<User>>(MockBehavior.Strict);
        accessTokenGenerator = new Mock<IAccessTokenGenerator>(MockBehavior.Strict);

        sut = new AuthenticationService(
            userAuthenticationDao.Object,
            passwordHasher.Object,
            accessTokenGenerator.Object);
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
            generator => generator.Issue(It.IsAny<User>(), It.IsAny<Tenant>()),
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
            generator => generator.Issue(It.IsAny<User>(), It.IsAny<Tenant>()),
            Times.Never);
    }

    [Test]
    public async Task LoginAsync_WhenCredentialsValid_ReturnsTokenFromGenerator()
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
        TokenResponseDto expectedToken = new() { AccessToken = "issued.jwt.token" };

        userAuthenticationDao
            .Setup(dao => dao.ReadUserWithTenantByUserNameAsync(request.Username))
            .ReturnsAsync(userWithTenant);
        passwordHasher
            .Setup(hasher => hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password))
            .Returns(PasswordVerificationResult.Success);
        accessTokenGenerator
            .Setup(generator => generator.Issue(user, tenant))
            .Returns(expectedToken);

        TokenResponseDto? token = await sut.LoginAsync(request);

        Assert.That(token, Is.SameAs(expectedToken));
    }

    [Test]
    public async Task LoginAsync_WhenPasswordVerificationSucceedsRehashNeeded_StillReturnsToken()
    {
        LoginCredentialsDto request = new() { Username = "known_user", Password = "correct_pass" };
        User user = new() { Id = Guid.NewGuid(), UserName = request.Username, PasswordHash = "old_hash" };
        Tenant tenant = new() { Id = Guid.NewGuid(), Name = "TenantOne", Timezone = "America/La_Paz" };
        TokenResponseDto expectedToken = new() { AccessToken = "issued.jwt.token" };

        userAuthenticationDao
            .Setup(dao => dao.ReadUserWithTenantByUserNameAsync(request.Username))
            .ReturnsAsync(new UserWithTenant(user, tenant));
        passwordHasher
            .Setup(hasher => hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password))
            .Returns(PasswordVerificationResult.SuccessRehashNeeded);
        accessTokenGenerator
            .Setup(generator => generator.Issue(user, tenant))
            .Returns(expectedToken);

        TokenResponseDto? token = await sut.LoginAsync(request);

        Assert.That(token, Is.SameAs(expectedToken));
    }
}
