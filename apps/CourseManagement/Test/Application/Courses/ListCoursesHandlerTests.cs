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
public class ListCoursesHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private Mock<ICourseDao> courseDao = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<IMapper> mapper = null!;
    private ListCoursesHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        courseDao = new Mock<ICourseDao>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        mapper = new Mock<IMapper>(MockBehavior.Strict);
        claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        handler = new ListCoursesHandler(courseDao.Object, claimContext.Object, mapper.Object);
    }

    [Test]
    public async Task Handle_ReturnsFoundWithMappedCourses()
    {
        var sourceCourses = new List<Course> { new() { Id = Guid.NewGuid(), Name = "A", TenantId = TenantId } };
        var mappedDtos = new List<GetCourseDto> { new() { Id = sourceCourses[0].Id, Name = "A" } };

        courseDao.Setup(dao => dao.GetCoursesByTenantIdAsync(TenantId)).ReturnsAsync(sourceCourses);
        mapper.Setup(map => map.Map<List<Course>, List<GetCourseDto>>(sourceCourses)).Returns(mappedDtos);

        ListCoursesResult result = await handler.Handle(new ListCoursesQuery());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<ListCoursesResult.Found>());
            Assert.That(((ListCoursesResult.Found)result).Courses, Is.SameAs(mappedDtos));
        });
    }
}
