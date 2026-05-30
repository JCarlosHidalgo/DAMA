namespace Backend.Events;

public sealed record ClassDeletedEvent(
    Guid EventId,
    string EventType,
    DateTime OccurredAt,
    Guid AggregateId,
    ClassDeletedEventData Data);

public sealed record ClassDeletedEventData(
    Guid ClassId,
    Guid TenantId);
