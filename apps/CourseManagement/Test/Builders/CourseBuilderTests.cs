using Backend.Builders;
using Backend.Dtos.Courses.Input;
using Backend.Entities.Courses;

namespace Test.Builders;

[TestFixture]
public class CourseBuilderTests
{
    private CourseBuilder _builder = null!;

    [SetUp]
    public void SetUp() => _builder = new CourseBuilder();

    [Test]
    public void BuildCourse_CopiesNameAndAssignsTenantAndId()
    {
        var tenantId = Guid.NewGuid();
        var payload = new CreateCourseDto { Name = "Curso Demo" };

        Course result = _builder.BuildCourse(tenantId, payload);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Name, Is.EqualTo("Curso Demo"));
            Assert.That(result.TenantId, Is.EqualTo(tenantId));
        });
    }

    [Test]
    public void BuildCourse_GeneratesUniqueIdPerInvocation()
    {
        var payload = new CreateCourseDto { Name = "Curso" };

        Course first = _builder.BuildCourse(Guid.NewGuid(), payload);
        Course second = _builder.BuildCourse(Guid.NewGuid(), payload);

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }
}
