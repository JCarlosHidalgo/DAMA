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
    private Mock<IPasswordHasher<User>> passwordHasher = null!;

    private UserEntityBuilder sut = null!;

    [SetUp]
    public void SetUp()
    {
        passwordHasher = new Mock<IPasswordHasher<User>>(MockBehavior.Strict);
        sut = new UserEntityBuilder(passwordHasher.Object);
    }

    [Test]
    public void BuildUser_WithGivenRequestAndRole_AssignsHashedPasswordAndFreshIdentity()
    {
        RegisterCredentialsDto request = new() { Username = "any_username", Password = "plain_pass" };
        passwordHasher
            .Setup(hasher => hasher.HashPassword(It.IsAny<User>(), request.Password))
            .Returns("hashed_value");

        User user = sut.BuildUser(request, UserRole.Student);

        Assert.Multiple(() =>
        {
            Assert.That(user.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(user.UserName, Is.EqualTo(request.Username));
            Assert.That(user.PasswordHash, Is.EqualTo("hashed_value"));
            Assert.That(user.Role, Is.EqualTo(UserRole.Student.Value));
            Assert.That(user.IsDeleted, Is.False);
        });
        passwordHasher.Verify(
            hasher => hasher.HashPassword(It.IsAny<User>(), request.Password),
            Times.Once);
    }

    [Test]
    public void BuildUser_AssignsRoleValueMatchingProvidedRole()
    {
        RegisterCredentialsDto request = new() { Username = "teach_one", Password = "plain_pass" };
        passwordHasher
            .Setup(hasher => hasher.HashPassword(It.IsAny<User>(), request.Password))
            .Returns("hashed_value");

        User user = sut.BuildUser(request, UserRole.Teacher);

        Assert.That(user.Role, Is.EqualTo(UserRole.Teacher.Value));
    }

    [Test]
    public void BuildUser_CalledTwice_ProducesDistinctIds()
    {
        RegisterCredentialsDto request = new() { Username = "user_one", Password = "plain_pass" };
        passwordHasher
            .Setup(hasher => hasher.HashPassword(It.IsAny<User>(), request.Password))
            .Returns("hashed_value");

        User first = sut.BuildUser(request, UserRole.Student);
        User second = sut.BuildUser(request, UserRole.Student);

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [Test]
    public void BuildTenantDomain_WithGivenIds_ReturnsTenantDomainWithSameIds()
    {
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var tenantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        TenantDomain tenantDomain = sut.BuildTenantDomain(userId, tenantId);

        Assert.Multiple(() =>
        {
            Assert.That(tenantDomain.UserId, Is.EqualTo(userId));
            Assert.That(tenantDomain.TenantId, Is.EqualTo(tenantId));
        });
    }
}
