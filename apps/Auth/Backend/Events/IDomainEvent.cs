namespace Backend.Events;

public interface IDomainEvent
{
    Guid EventId { get; }
    string EventType { get; }
    DateTime OccurredAt { get; }
    Guid AggregateId { get; }
}
