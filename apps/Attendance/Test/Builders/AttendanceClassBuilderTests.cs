using Backend.Builders;
using Backend.Dtos.Attendance.Input;
using Backend.Entities.Attendance;
using Backend.Transporters.Entities;

namespace Test.Builders;

[TestFixture]
public class AttendanceClassBuilderTests
{
    private AttendanceClassBuilder _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new AttendanceClassBuilder();

    [Test]
    public void BuildScheduledAttendance_MapsAllFields()
    {
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        DateOnly classDate = new(2026, 5, 24);
        ScheduledAttendanceDto request = new() { ClassId = classId, CourseName = "Course One" };
        ClassExistenceMeta metadata = new(new TimeOnly(8, 0), new TimeOnly(9, 30), classDate, 0);

        ScheduledClassAttendance attendance = _sut.BuildScheduledAttendance(
            tenantId, studentId, "Student", classDate, request, metadata);

        Assert.Multiple(() =>
        {
            Assert.That(attendance.TenantId, Is.EqualTo(tenantId));
            Assert.That(attendance.ClassId, Is.EqualTo(classId));
            Assert.That(attendance.ClassDate, Is.EqualTo(classDate));
            Assert.That(attendance.StartTime, Is.EqualTo(new TimeOnly(8, 0)));
            Assert.That(attendance.EndTime, Is.EqualTo(new TimeOnly(9, 30)));
            Assert.That(attendance.CourseName, Is.EqualTo("Course One"));
            Assert.That(attendance.StudentId, Is.EqualTo(studentId));
            Assert.That(attendance.StudentName, Is.EqualTo("Student"));
        });
    }

    [Test]
    public void BuildUniqueAttendance_MapsAllFields()
    {
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        DateOnly classDate = new(2026, 5, 24);
        UniqueAttendanceDto request = new() { ClassId = classId, CourseName = "Course One" };
        ClassExistenceMeta metadata = new(new TimeOnly(10, 0), new TimeOnly(11, 30), classDate, 0);

        UniqueClassAttendance attendance = _sut.BuildUniqueAttendance(
            tenantId, studentId, "Student", classDate, request, metadata);

        Assert.Multiple(() =>
        {
            Assert.That(attendance.TenantId, Is.EqualTo(tenantId));
            Assert.That(attendance.ClassId, Is.EqualTo(classId));
            Assert.That(attendance.ClassDate, Is.EqualTo(classDate));
            Assert.That(attendance.StartTime, Is.EqualTo(new TimeOnly(10, 0)));
            Assert.That(attendance.EndTime, Is.EqualTo(new TimeOnly(11, 30)));
            Assert.That(attendance.CourseName, Is.EqualTo("Course One"));
            Assert.That(attendance.StudentId, Is.EqualTo(studentId));
            Assert.That(attendance.StudentName, Is.EqualTo("Student"));
        });
    }

    [Test]
    public void BuildPage_PropagatesFields()
    {
        List<string> items = ["a", "b"];

        Backend.Common.PageDto<string> page = _sut.BuildPage(currentIndex: 3, maxIndex: 7, items);

        Assert.Multiple(() =>
        {
            Assert.That(page.CurrentIndex, Is.EqualTo(3));
            Assert.That(page.MaxIndex, Is.EqualTo(7));
            Assert.That(page.Items, Is.SameAs(items));
        });
    }
}
