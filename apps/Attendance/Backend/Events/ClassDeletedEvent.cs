namespace Backend.Events;

public sealed class ClassDeletedEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
    public Guid AggregateId { get; set; }
    public ClassDeletedData Data { get; set; } = default!;
}

public sealed class ClassDeletedData
{
    public Guid ClassId { get; set; }
    public Guid TenantId { get; set; }
}
