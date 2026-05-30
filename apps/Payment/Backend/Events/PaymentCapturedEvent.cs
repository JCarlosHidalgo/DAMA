namespace Backend.Events;

public sealed record PaymentCapturedEvent
{
    public Guid EventId { get; init; }

    public string EventType { get; init; } = "PaymentCaptured";

    public DateTime OccurredAt { get; init; }

    public Guid AggregateId { get; init; }

    public PaymentCapturedData Data { get; init; } = default!;
}

public sealed record PaymentCapturedData(
    Guid TenantId,
    Guid StudentId,
    int Quantity,
    string ExternalReference);
