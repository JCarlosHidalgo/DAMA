namespace Backend.Events;

public sealed class DebtExpiredEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
    public Guid AggregateId { get; set; }
    public DebtExpiredData Data { get; set; } = default!;
}

public sealed class DebtExpiredData
{
    public Guid PendingId { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
}
