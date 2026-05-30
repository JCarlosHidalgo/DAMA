namespace Backend.Events;

public sealed class CourseDeletedEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
    public Guid AggregateId { get; set; }
    public CourseDeletedData Data { get; set; } = default!;
}

public sealed class CourseDeletedData
{
    public Guid CourseId { get; set; }
    public Guid TenantId { get; set; }
    public List<Guid> ClassIds { get; set; } = new();
}
