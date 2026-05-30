namespace Backend.Events;

public sealed record CourseDeletedEvent(
    Guid EventId,
    string EventType,
    DateTime OccurredAt,
    Guid AggregateId,
    CourseDeletedEventData Data);

public sealed record CourseDeletedEventData(
    Guid CourseId,
    Guid TenantId,
    IReadOnlyList<Guid> ClassIds);
