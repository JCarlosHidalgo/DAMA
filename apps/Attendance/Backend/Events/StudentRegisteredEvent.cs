namespace Backend.Events;

public sealed class StudentRegisteredEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
    public Guid AggregateId { get; set; }
    public StudentRegisteredData Data { get; set; } = default!;
}

public sealed class StudentRegisteredData
{
    public Guid StudentId { get; set; }
    public Guid TenantId { get; set; }
    public string UserName { get; set; } = default!;
    public DateTime RegisteredAt { get; set; }
}
