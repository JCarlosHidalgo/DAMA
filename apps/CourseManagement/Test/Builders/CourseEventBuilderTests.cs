using System.Text.Json;

using Backend.Builders;
using Backend.Entities;
using Backend.Events;

namespace Test.Builders;

[TestFixture]
public class CourseEventBuilderTests
{
    private CourseEventBuilder _builder = null!;

    [SetUp]
    public void SetUp() => _builder = new CourseEventBuilder();

    [Test]
    public void BuildCourseDeleted_PopulatesOutboxFieldsAndStampsTimestamp()
    {
        var tenantId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var classIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        DateTime before = DateTime.UtcNow;

        OutboxEvent outboxEvent = _builder.BuildCourseDeleted(tenantId, courseId, classIds);

        DateTime after = DateTime.UtcNow;
        Assert.Multiple(() =>
        {
            Assert.That(outboxEvent.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(outboxEvent.AggregateType, Is.EqualTo("Course"));
            Assert.That(outboxEvent.AggregateId, Is.EqualTo(courseId));
            Assert.That(outboxEvent.EventType, Is.EqualTo("CourseDeleted"));
            Assert.That(outboxEvent.RoutingKey, Is.EqualTo("course.deleted"));
            Assert.That(outboxEvent.OccurredAt, Is.InRange(before, after));
        });
    }

    [Test]
    public void BuildCourseDeleted_PayloadEventIdMatchesOutboxRowId()
    {
        OutboxEvent outboxEvent = _builder.BuildCourseDeleted(Guid.NewGuid(), Guid.NewGuid(), new List<Guid>());

        CourseDeletedEvent? payload = JsonSerializer.Deserialize<CourseDeletedEvent>(outboxEvent.Payload);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.EventId, Is.EqualTo(outboxEvent.Id));
    }

    [Test]
    public void BuildCourseDeleted_PayloadCarriesCourseAndTenantAndClassIds()
    {
        var tenantId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var classIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        OutboxEvent outboxEvent = _builder.BuildCourseDeleted(tenantId, courseId, classIds);

        CourseDeletedEvent payload = JsonSerializer.Deserialize<CourseDeletedEvent>(outboxEvent.Payload)!;
        Assert.Multiple(() =>
        {
            Assert.That(payload.EventType, Is.EqualTo("CourseDeleted"));
            Assert.That(payload.AggregateId, Is.EqualTo(courseId));
            Assert.That(payload.Data.CourseId, Is.EqualTo(courseId));
            Assert.That(payload.Data.TenantId, Is.EqualTo(tenantId));
            Assert.That(payload.Data.ClassIds, Is.EquivalentTo(classIds));
        });
    }

    [Test]
    public void BuildClassDeleted_PopulatesOutboxFieldsAndStampsTimestamp()
    {
        var tenantId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        DateTime before = DateTime.UtcNow;

        OutboxEvent outboxEvent = _builder.BuildClassDeleted(tenantId, classId);

        DateTime after = DateTime.UtcNow;
        Assert.Multiple(() =>
        {
            Assert.That(outboxEvent.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(outboxEvent.AggregateType, Is.EqualTo("Class"));
            Assert.That(outboxEvent.AggregateId, Is.EqualTo(classId));
            Assert.That(outboxEvent.EventType, Is.EqualTo("ClassDeleted"));
            Assert.That(outboxEvent.RoutingKey, Is.EqualTo("class.deleted"));
            Assert.That(outboxEvent.OccurredAt, Is.InRange(before, after));
        });
    }

    [Test]
    public void BuildClassDeleted_PayloadEventIdMatchesOutboxRowId()
    {
        OutboxEvent outboxEvent = _builder.BuildClassDeleted(Guid.NewGuid(), Guid.NewGuid());

        ClassDeletedEvent? payload = JsonSerializer.Deserialize<ClassDeletedEvent>(outboxEvent.Payload);

        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.EventId, Is.EqualTo(outboxEvent.Id));
    }

    [Test]
    public void BuildClassDeleted_PayloadCarriesClassAndTenant()
    {
        var tenantId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        OutboxEvent outboxEvent = _builder.BuildClassDeleted(tenantId, classId);

        ClassDeletedEvent payload = JsonSerializer.Deserialize<ClassDeletedEvent>(outboxEvent.Payload)!;
        Assert.Multiple(() =>
        {
            Assert.That(payload.EventType, Is.EqualTo("ClassDeleted"));
            Assert.That(payload.AggregateId, Is.EqualTo(classId));
            Assert.That(payload.Data.ClassId, Is.EqualTo(classId));
            Assert.That(payload.Data.TenantId, Is.EqualTo(tenantId));
        });
    }

    [Test]
    public void BuildCourseDeleted_GeneratesUniqueEventIdPerInvocation()
    {
        OutboxEvent first = _builder.BuildCourseDeleted(Guid.NewGuid(), Guid.NewGuid(), new List<Guid>());
        OutboxEvent second = _builder.BuildCourseDeleted(Guid.NewGuid(), Guid.NewGuid(), new List<Guid>());

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [Test]
    public void BuildClassDeleted_GeneratesUniqueEventIdPerInvocation()
    {
        OutboxEvent first = _builder.BuildClassDeleted(Guid.NewGuid(), Guid.NewGuid());
        OutboxEvent second = _builder.BuildClassDeleted(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }
}
