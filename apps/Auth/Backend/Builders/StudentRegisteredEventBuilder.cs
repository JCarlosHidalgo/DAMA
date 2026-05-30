using System.Text.Json;

using Backend.Entities;
using Backend.Entities.Users;
using Backend.Events;

namespace Backend.Builders;

public sealed class StudentRegisteredEventBuilder : IStudentRegisteredEventBuilder
{
    private const string EventTypeName = "StudentRegistered";
    private const string RoutingKey = "student.registered";
    private const string AggregateTypeName = "Student";

    public OutboxEvent Build(User user, Guid tenantId)
    {
        Guid eventId = Guid.NewGuid();
        DateTime occurredAt = DateTime.UtcNow;

        StudentRegisteredEvent domainEvent = new StudentRegisteredEvent
        {
            EventId = eventId,
            EventType = EventTypeName,
            OccurredAt = occurredAt,
            AggregateId = user.Id,
            Data = new StudentRegisteredEventData(
                StudentId: user.Id,
                TenantId: tenantId,
                UserName: user.UserName,
                RegisteredAt: occurredAt)
        };

        return new OutboxEvent
        {
            Id = eventId,
            AggregateType = AggregateTypeName,
            AggregateId = user.Id,
            EventType = EventTypeName,
            RoutingKey = RoutingKey,
            Payload = JsonSerializer.Serialize(domainEvent),
            OccurredAt = occurredAt
        };
    }
}
