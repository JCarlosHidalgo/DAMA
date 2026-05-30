using AutoMapper;

using Backend.Application.Courses;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.Dtos.Courses.Input;
using Backend.Dtos.Courses.Output;
using Backend.Entities.Courses;
using Backend.Results.Courses;

using Moq;

namespace Test.Application.Courses;

[TestFixture]
public class UpdateCourseHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CourseId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<ICourseDao> courseDao = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<IMapper> mapper = null!;
    private UpdateCourseHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        courseDao = new Mock<ICourseDao>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        mapper = new Mock<IMapper>(MockBehavior.Strict);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        handler = new UpdateCourseHandler(courseDao.Object, claimContext.Object, mapper.Object);
    }

    [Test]
    public async Task Handle_WhenUpdateFails_ReturnsNotFound()
    {
        courseDao.Setup(dao => dao.UpdateForTenantAsync(TenantId, CourseId, "Nuevo")).ReturnsAsync(false);

        UpdateCourseResult result = await handler.Handle(new UpdateCourseCommand(CourseId, new UpdateCourseDto { Name = "Nuevo" }));

        Assert.That(result, Is.InstanceOf<UpdateCourseResult.NotFound>());
    }

    [Test]
    public async Task Handle_WhenUpdateSucceedsButGetByIdReturnsNull_ReturnsNotFound()
    {
        courseDao.Setup(dao => dao.UpdateForTenantAsync(TenantId, CourseId, "Nuevo")).ReturnsAsync(true);
        courseDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, CourseId)).ReturnsAsync((Course?)null);

        UpdateCourseResult result = await handler.Handle(new UpdateCourseCommand(CourseId, new UpdateCourseDto { Name = "Nuevo" }));

        Assert.That(result, Is.InstanceOf<UpdateCourseResult.NotFound>());
    }

    [Test]
    public async Task Handle_WhenUpdateSucceeds_ReturnsUpdatedWithMappedDto()
    {
        var refreshed = new Course { Id = CourseId, Name = "Nuevo", TenantId = TenantId };
        var mappedDto = new GetCourseDto { Id = CourseId, Name = "Nuevo" };

        courseDao.Setup(dao => dao.UpdateForTenantAsync(TenantId, CourseId, "Nuevo")).ReturnsAsync(true);
        courseDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, CourseId)).ReturnsAsync(refreshed);
        mapper.Setup(map => map.Map<GetCourseDto>(refreshed)).Returns(mappedDto);

        UpdateCourseResult result = await handler.Handle(new UpdateCourseCommand(CourseId, new UpdateCourseDto { Name = "Nuevo" }));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<UpdateCourseResult.Updated>());
            Assert.That(((UpdateCourseResult.Updated)result).Course, Is.SameAs(mappedDto));
        });
    }
}
