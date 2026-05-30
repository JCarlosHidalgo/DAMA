using System.Text.Json;

using Backend.Entities;
using Backend.Events;

namespace Backend.Builders;

public sealed class CourseEventBuilder : ICourseEventBuilder
{
    private const string CourseDeletedEventType = "CourseDeleted";
    private const string CourseDeletedRoutingKey = "course.deleted";
    private const string CourseAggregateType = "Course";

    private const string ClassDeletedEventType = "ClassDeleted";
    private const string ClassDeletedRoutingKey = "class.deleted";
    private const string ClassAggregateType = "Class";

    public OutboxEvent BuildCourseDeleted(Guid tenantId, Guid courseId, IReadOnlyList<Guid> classIds)
    {
        Guid eventId = Guid.NewGuid();
        DateTime occurredAt = DateTime.UtcNow;

        CourseDeletedEvent payload = new CourseDeletedEvent(
            EventId: eventId,
            EventType: CourseDeletedEventType,
            OccurredAt: occurredAt,
            AggregateId: courseId,
            Data: new CourseDeletedEventData(courseId, tenantId, classIds));

        return new OutboxEvent
        {
            Id = eventId,
            AggregateType = CourseAggregateType,
            AggregateId = courseId,
            EventType = CourseDeletedEventType,
            RoutingKey = CourseDeletedRoutingKey,
            Payload = JsonSerializer.Serialize(payload),
            OccurredAt = occurredAt
        };
    }

    public OutboxEvent BuildClassDeleted(Guid tenantId, Guid classId)
    {
        Guid eventId = Guid.NewGuid();
        DateTime occurredAt = DateTime.UtcNow;

        ClassDeletedEvent payload = new ClassDeletedEvent(
            EventId: eventId,
            EventType: ClassDeletedEventType,
            OccurredAt: occurredAt,
            AggregateId: classId,
            Data: new ClassDeletedEventData(classId, tenantId));

        return new OutboxEvent
        {
            Id = eventId,
            AggregateType = ClassAggregateType,
            AggregateId = classId,
            EventType = ClassDeletedEventType,
            RoutingKey = ClassDeletedRoutingKey,
            Payload = JsonSerializer.Serialize(payload),
            OccurredAt = occurredAt
        };
    }
}
