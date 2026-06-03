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

    private Mock<ICourseDao> _courseDao = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IMapper> _mapper = null!;
    private ListCoursesHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _courseDao = new Mock<ICourseDao>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _mapper = new Mock<IMapper>(MockBehavior.Strict);
        _claimContext.SetupGet(context => context.TenantId).Returns(TenantId);
        _handler = new ListCoursesHandler(_courseDao.Object, _claimContext.Object, _mapper.Object);
    }

    [Test]
    public async Task Handle_ReturnsFoundWithMappedCourses()
    {
        var sourceCourses = new List<Course> { new() { Id = Guid.NewGuid(), Name = "A", TenantId = TenantId } };
        var mappedDtos = new List<GetCourseDto> { new() { Id = sourceCourses[0].Id, Name = "A" } };

        _courseDao.Setup(dao => dao.GetCoursesByTenantIdAsync(TenantId)).ReturnsAsync(sourceCourses);
        _mapper.Setup(map => map.Map<List<Course>, List<GetCourseDto>>(sourceCourses)).Returns(mappedDtos);

        ListCoursesResult result = await _handler.Handle(new ListCoursesQuery());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<ListCoursesResult.Found>());
            Assert.That(((ListCoursesResult.Found)result).Courses, Is.SameAs(mappedDtos));
        });
    }
}
