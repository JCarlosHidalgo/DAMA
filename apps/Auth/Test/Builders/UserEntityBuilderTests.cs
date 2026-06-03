using Backend.Builders;
using Backend.Dtos.Users.Input;
using Backend.Entities.Tenants;
using Backend.Entities.Users;

using Microsoft.AspNetCore.Identity;

using Moq;

namespace Test.Builders;

[TestFixture]
public class UserEntityBuilderTests
{
    private Mock<IPasswordHasher<User>> _passwordHasher = null!;

    private UserEntityBuilder _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _passwordHasher = new Mock<IPasswordHasher<User>>(MockBehavior.Strict);
        _sut = new UserEntityBuilder(_passwordHasher.Object);
    }

    [Test]
    public void BuildUser_WithGivenRequestAndRole_AssignsHashedPasswordAndFreshIdentity()
    {
        RegisterCredentialsDto request = new() { Username = "any_username", Password = "plain_pass" };
        _passwordHasher
            .Setup(hasher => hasher.HashPassword(It.IsAny<User>(), request.Password))
            .Returns("hashed_value");

        User user = _sut.BuildUser(request, UserRole.Student);

        Assert.Multiple(() =>
        {
            Assert.That(user.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(user.UserName, Is.EqualTo(request.Username));
            Assert.That(user.PasswordHash, Is.EqualTo("hashed_value"));
            Assert.That(user.Role, Is.EqualTo(UserRole.Student.Value));
            Assert.That(user.IsDeleted, Is.False);
        });
        _passwordHasher.Verify(
            hasher => hasher.HashPassword(It.IsAny<User>(), request.Password),
            Times.Once);
    }

    [Test]
    public void BuildUser_AssignsRoleValueMatchingProvidedRole()
    {
        RegisterCredentialsDto request = new() { Username = "teach_one", Password = "plain_pass" };
        _passwordHasher
            .Setup(hasher => hasher.HashPassword(It.IsAny<User>(), request.Password))
            .Returns("hashed_value");

        User user = _sut.BuildUser(request, UserRole.Teacher);

        Assert.That(user.Role, Is.EqualTo(UserRole.Teacher.Value));
    }

    [Test]
    public void BuildUser_CalledTwice_ProducesDistinctIds()
    {
        RegisterCredentialsDto request = new() { Username = "user_one", Password = "plain_pass" };
        _passwordHasher
            .Setup(hasher => hasher.HashPassword(It.IsAny<User>(), request.Password))
            .Returns("hashed_value");

        User first = _sut.BuildUser(request, UserRole.Student);
        User second = _sut.BuildUser(request, UserRole.Student);

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [Test]
    public void BuildTenantDomain_WithGivenIds_ReturnsTenantDomainWithSameIds()
    {
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var tenantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        TenantDomain tenantDomain = _sut.BuildTenantDomain(userId, tenantId);

        Assert.Multiple(() =>
        {
            Assert.That(tenantDomain.UserId, Is.EqualTo(userId));
            Assert.That(tenantDomain.TenantId, Is.EqualTo(tenantId));
        });
    }
}
