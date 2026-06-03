using Backend.Builders;
using Backend.Dtos.Scheduleds.Input;
using Backend.Dtos.Uniques.Input;
using Backend.Entities;
using Backend.Entities.Scheduleds;
using Backend.Entities.Uniques;

namespace Test.Builders;

[TestFixture]
public class ClassBuilderTests
{
    private ClassBuilder _builder = null!;

    [SetUp]
    public void SetUp() => _builder = new ClassBuilder();

    [Test]
    public void BuildScheduledClass_CopiesPayloadAndAssignsTenantGroupAndTeachers()
    {
        var tenantId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var payload = new CreateScheduledClassDto
        {
            DayOfWeekIndex = 3,
            MaxStudentLimit = 30,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30),
            CourseId = courseId,
            GroupId = groupId,
            Teachers = []
        };
        var teachers = new List<ClassTeacher>
        {
            new() { TeacherId = Guid.NewGuid(), TeacherName = "Profesor" }
        };

        ScheduledClass result = _builder.BuildScheduledClass(tenantId, courseId, groupId, payload, teachers);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.DayOfWeekIndex, Is.EqualTo(3));
            Assert.That(result.MaxStudentLimit, Is.EqualTo(30));
            Assert.That(result.StartTime, Is.EqualTo(new TimeOnly(9, 0)));
            Assert.That(result.EndTime, Is.EqualTo(new TimeOnly(10, 30)));
            Assert.That(result.CourseId, Is.EqualTo(courseId));
            Assert.That(result.GroupId, Is.EqualTo(groupId));
            Assert.That(result.TenantId, Is.EqualTo(tenantId));
            Assert.That(result.Teachers, Is.SameAs(teachers));
        });
    }

    [Test]
    public void BuildScheduledClass_GeneratesUniqueIdPerInvocation()
    {
        var payload = new CreateScheduledClassDto
        {
            DayOfWeekIndex = 1,
            MaxStudentLimit = 0,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(9, 0),
            CourseId = Guid.NewGuid(),
            GroupId = Guid.NewGuid(),
            Teachers = []
        };
        var teachers = new List<ClassTeacher>();

        ScheduledClass first = _builder.BuildScheduledClass(Guid.NewGuid(), payload.CourseId, payload.GroupId, payload, teachers);
        ScheduledClass second = _builder.BuildScheduledClass(Guid.NewGuid(), payload.CourseId, payload.GroupId, payload, teachers);

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [Test]
    public void BuildUniqueClass_CopiesPayloadAndAssignsTenantGroupAndTeachers()
    {
        var tenantId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var date = new DateOnly(2025, 7, 4);
        var payload = new CreateUniqueClassDto
        {
            Date = date,
            MaxStudentLimit = 30,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 30),
            CourseId = courseId,
            GroupId = groupId,
            Teachers = []
        };
        var teachers = new List<ClassTeacher>
        {
            new() { TeacherId = Guid.NewGuid(), TeacherName = "Profesor" }
        };

        UniqueClass result = _builder.BuildUniqueClass(tenantId, courseId, groupId, payload, teachers);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Date, Is.EqualTo(date));
            Assert.That(result.MaxStudentLimit, Is.EqualTo(30));
            Assert.That(result.StartTime, Is.EqualTo(new TimeOnly(14, 0)));
            Assert.That(result.EndTime, Is.EqualTo(new TimeOnly(15, 30)));
            Assert.That(result.CourseId, Is.EqualTo(courseId));
            Assert.That(result.GroupId, Is.EqualTo(groupId));
            Assert.That(result.TenantId, Is.EqualTo(tenantId));
            Assert.That(result.Teachers, Is.SameAs(teachers));
        });
    }

    [Test]
    public void BuildUniqueClass_GeneratesUniqueIdPerInvocation()
    {
        var payload = new CreateUniqueClassDto
        {
            Date = new DateOnly(2025, 1, 1),
            MaxStudentLimit = 0,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(9, 0),
            CourseId = Guid.NewGuid(),
            GroupId = Guid.NewGuid(),
            Teachers = []
        };
        var teachers = new List<ClassTeacher>();

        UniqueClass first = _builder.BuildUniqueClass(Guid.NewGuid(), payload.CourseId, payload.GroupId, payload, teachers);
        UniqueClass second = _builder.BuildUniqueClass(Guid.NewGuid(), payload.CourseId, payload.GroupId, payload, teachers);

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }
}
