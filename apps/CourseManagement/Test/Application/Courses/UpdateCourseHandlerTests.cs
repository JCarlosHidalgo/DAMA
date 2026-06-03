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

    private Mock<ICourseDao> _courseDao = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IMapper> _mapper = null!;
    private UpdateCourseHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _courseDao = new Mock<ICourseDao>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        _handler = new UpdateCourseHandler(_courseDao.Object, _claimContext.Object, _mapper.Object);
    }

    [Test]
    public async Task Handle_WhenUpdateFails_ReturnsNotFound()
    {
        _courseDao.Setup(dao => dao.UpdateForTenantAsync(TenantId, CourseId, "Nuevo")).ReturnsAsync(false);

        UpdateCourseResult result = await _handler.Handle(new UpdateCourseCommand(CourseId, new UpdateCourseDto { Name = "Nuevo" }));

        Assert.That(result, Is.InstanceOf<UpdateCourseResult.NotFound>());
    }

    [Test]
    public async Task Handle_WhenUpdateSucceedsButGetByIdReturnsNull_ReturnsNotFound()
    {
        _courseDao.Setup(dao => dao.UpdateForTenantAsync(TenantId, CourseId, "Nuevo")).ReturnsAsync(true);
        _courseDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, CourseId)).ReturnsAsync((Course?)null);

        UpdateCourseResult result = await _handler.Handle(new UpdateCourseCommand(CourseId, new UpdateCourseDto { Name = "Nuevo" }));

        Assert.That(result, Is.InstanceOf<UpdateCourseResult.NotFound>());
    }

    [Test]
    public async Task Handle_WhenUpdateSucceeds_ReturnsUpdatedWithMappedDto()
    {
        var refreshed = new Course { Id = CourseId, Name = "Nuevo", TenantId = TenantId };
        var mappedDto = new GetCourseDto { Id = CourseId, Name = "Nuevo" };

        _courseDao.Setup(dao => dao.UpdateForTenantAsync(TenantId, CourseId, "Nuevo")).ReturnsAsync(true);
        _courseDao.Setup(dao => dao.GetByIdForTenantAsync(TenantId, CourseId)).ReturnsAsync(refreshed);
        _mapper.Setup(map => map.Map<GetCourseDto>(refreshed)).Returns(mappedDto);

        UpdateCourseResult result = await _handler.Handle(new UpdateCourseCommand(CourseId, new UpdateCourseDto { Name = "Nuevo" }));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<UpdateCourseResult.Updated>());
            Assert.That(((UpdateCourseResult.Updated)result).Course, Is.SameAs(mappedDto));
        });
    }
}
