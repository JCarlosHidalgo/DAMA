using AutoMapper;

using Backend.Application.Courses;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.Dtos.Courses.Output;
using Backend.Entities.Courses;
using Backend.Results.Courses;

using Moq;

namespace Test.Application.Courses;

[TestFixture]
public class GetCourseByIdHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CourseId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<ICourseDao> courseDao = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<IMapper> mapper = null!;
    private GetCourseByIdHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        courseDao = new Mock<ICourseDao>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        mapper = new Mock<IMapper>(MockBehavior.Strict);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        handler = new GetCourseByIdHandler(courseDao.Object, claimContext.Object, mapper.Object);
    }

    [Test]
    public async Task Handle_WhenDaoReturnsNull_ReturnsNotFound()
    {
        courseDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, CourseId)).ReturnsAsync((Course?)null);

        GetCourseByIdResult result = await handler.Handle(new GetCourseByIdQuery(CourseId));

        Assert.That(result, Is.InstanceOf<GetCourseByIdResult.NotFound>());
    }

    [Test]
    public async Task Handle_WhenCourseExists_ReturnsFoundWithMappedDto()
    {
        var course = new Course { Id = CourseId, Name = "Curso", TenantId = TenantId };
        var mappedDto = new GetCourseDto { Id = CourseId, Name = "Curso" };
        courseDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, CourseId)).ReturnsAsync(course);
        mapper.Setup(map => map.Map<GetCourseDto>(course)).Returns(mappedDto);

        GetCourseByIdResult result = await handler.Handle(new GetCourseByIdQuery(CourseId));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<GetCourseByIdResult.Found>());
            Assert.That(((GetCourseByIdResult.Found)result).Course, Is.SameAs(mappedDto));
        });
    }
}
