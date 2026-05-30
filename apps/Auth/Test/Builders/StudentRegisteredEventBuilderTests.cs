using System.Text.Json;

using Backend.Builders;
using Backend.Entities;
using Backend.Entities.Users;

namespace Test.Builders;

[TestFixture]
public class StudentRegisteredEventBuilderTests
{
    private StudentRegisteredEventBuilder sut = null!;

    [SetUp]
    public void SetUp() => sut = new StudentRegisteredEventBuilder();

    [Test]
    public void Build_AssignsCanonicalEventMetadata()
    {
        User student = new()
        {
            Id = Guid.NewGuid(),
            UserName = "fresh_student",
            Role = UserRole.Student.Value
        };
        var tenantId = Guid.NewGuid();
        DateTime utcBeforeBuild = DateTime.UtcNow;

        OutboxEvent outboxEvent = sut.Build(student, tenantId);

        Assert.Multiple(() =>
        {
            Assert.That(outboxEvent.EventType, Is.EqualTo("StudentRegistered"));
            Assert.That(outboxEvent.RoutingKey, Is.EqualTo("student.registered"));
            Assert.That(outboxEvent.AggregateType, Is.EqualTo("Student"));
            Assert.That(outboxEvent.AggregateId, Is.EqualTo(student.Id));
            Assert.That(outboxEvent.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(outboxEvent.OccurredAt,
                Is.InRange(utcBeforeBuild.AddSeconds(-1), utcBeforeBuild.AddSeconds(5)));
            Assert.That(outboxEvent.PublishedAt, Is.Null);
            Assert.That(outboxEvent.LeasedUntil, Is.Null);
            Assert.That(outboxEvent.Attempts, Is.EqualTo(0));
            Assert.That(outboxEvent.LastError, Is.Null);
        });
    }

    [Test]
    public void Build_PayloadContainsEventIdEqualToOutboxRowId()
    {
        User student = new() { Id = Guid.NewGuid(), UserName = "fresh_student" };
        var tenantId = Guid.NewGuid();

        OutboxEvent outboxEvent = sut.Build(student, tenantId);

        using var document = JsonDocument.Parse(outboxEvent.Payload);
        Guid payloadEventId = document.RootElement.GetProperty("EventId").GetGuid();
        Guid payloadAggregateId = document.RootElement.GetProperty("AggregateId").GetGuid();

        Assert.Multiple(() =>
        {
            Assert.That(payloadEventId, Is.EqualTo(outboxEvent.Id));
            Assert.That(payloadAggregateId, Is.EqualTo(student.Id));
        });
    }

    [Test]
    public void Build_PayloadCarriesNestedStudentData()
    {
        User student = new() { Id = Guid.NewGuid(), UserName = "fresh_student" };
        var tenantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        OutboxEvent outboxEvent = sut.Build(student, tenantId);

        using var document = JsonDocument.Parse(outboxEvent.Payload);
        JsonElement data = document.RootElement.GetProperty("Data");
        Assert.Multiple(() =>
        {
            Assert.That(data.GetProperty("StudentId").GetGuid(), Is.EqualTo(student.Id));
            Assert.That(data.GetProperty("TenantId").GetGuid(), Is.EqualTo(tenantId));
            Assert.That(data.GetProperty("UserName").GetString(), Is.EqualTo("fresh_student"));
            Assert.That(data.GetProperty("RegisteredAt").GetDateTime(), Is.EqualTo(outboxEvent.OccurredAt));
        });
    }

    [Test]
    public void Build_CalledTwice_ProducesDistinctEventIds()
    {
        User student = new() { Id = Guid.NewGuid(), UserName = "fresh_student" };
        var tenantId = Guid.NewGuid();

        OutboxEvent first = sut.Build(student, tenantId);
        OutboxEvent second = sut.Build(student, tenantId);

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }
}
