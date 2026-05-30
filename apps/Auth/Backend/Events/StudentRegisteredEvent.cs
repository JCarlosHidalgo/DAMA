namespace Backend.Events;

public sealed record StudentRegisteredEvent : IDomainEvent
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = "StudentRegistered";
    public DateTime OccurredAt { get; init; }
    public Guid AggregateId { get; init; }
    public StudentRegisteredEventData Data { get; init; } = default!;
}

public sealed record StudentRegisteredEventData(
    Guid StudentId,
    Guid TenantId,
    string UserName,
    DateTime RegisteredAt);
