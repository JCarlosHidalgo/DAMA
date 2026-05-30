using Backend.Application.Courses;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.Results.Courses;

using Moq;

namespace Test.Application.Courses;

[TestFixture]
public class CourseExistsHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CourseId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<ICourseDao> courseDao = null!;
    private Mock<IClaimContext> claimContext = null!;
    private CourseExistsHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        courseDao = new Mock<ICourseDao>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        handler = new CourseExistsHandler(courseDao.Object, claimContext.Object);
    }

    [Test]
    public async Task Handle_WhenCourseExists_ReturnsExists()
    {
        courseDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, CourseId)).ReturnsAsync(true);

        CourseExistsResult result = await handler.Handle(new CourseExistsQuery(CourseId));

        Assert.That(result, Is.InstanceOf<CourseExistsResult.Exists>());
    }

    [Test]
    public async Task Handle_WhenCourseDoesNotExist_ReturnsDoesNotExist()
    {
        courseDao.Setup(dao => dao.ExistsForTenantAsync(TenantId, CourseId)).ReturnsAsync(false);

        CourseExistsResult result = await handler.Handle(new CourseExistsQuery(CourseId));

        Assert.That(result, Is.InstanceOf<CourseExistsResult.DoesNotExist>());
    }
}
