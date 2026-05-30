using Backend.Claims;
using Backend.Entities.Users;
using Backend.Services.Concrete;

using Moq;

namespace Test.Services.Concrete;

[TestFixture]
public class ClaimContextExtensionsTests
{
    private static readonly Guid CallerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherStudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<IClaimContext> claimContext = null!;

    [SetUp]
    public void SetUp() => claimContext = new Mock<IClaimContext>(MockBehavior.Strict);

    [Test]
    public void IsStudentAccessingOtherStudent_WhenRoleIsNotStudent_ReturnsFalse()
    {
        claimContext.Setup(target => target.Role).Returns(UserRoles.Client);

        bool result = claimContext.Object.IsStudentAccessingOtherStudent(OtherStudentId);

        Assert.That(result, Is.False);
        claimContext.Verify(target => target.UserId, Times.Never);
    }

    [Test]
    public void IsStudentAccessingOtherStudent_WhenStudentAccessesOwnData_ReturnsFalse()
    {
        claimContext.Setup(target => target.Role).Returns(UserRoles.Student);
        claimContext.Setup(target => target.UserId).Returns(CallerUserId);

        bool result = claimContext.Object.IsStudentAccessingOtherStudent(CallerUserId);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsStudentAccessingOtherStudent_WhenStudentAccessesOtherStudent_ReturnsTrue()
    {
        claimContext.Setup(target => target.Role).Returns(UserRoles.Student);
        claimContext.Setup(target => target.UserId).Returns(CallerUserId);

        bool result = claimContext.Object.IsStudentAccessingOtherStudent(OtherStudentId);

        Assert.That(result, Is.True);
    }
}
