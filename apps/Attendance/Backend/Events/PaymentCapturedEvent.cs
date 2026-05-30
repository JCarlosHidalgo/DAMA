namespace Backend.Events;

public sealed class PaymentCapturedEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
    public Guid AggregateId { get; set; }
    public PaymentCapturedData Data { get; set; } = default!;
}

public sealed class PaymentCapturedData
{
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public int Quantity { get; set; }
    public string ExternalReference { get; set; } = default!;
}
