using DAMA.Software.MySqlOutbox;

using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities;

public sealed class OutboxEvent : IOutboxEvent, IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Text(64)]
    public string AggregateType { get; set; } = default!;

    [Identifier]
    public Guid AggregateId { get; set; }

    [Text(128)]
    public string EventType { get; set; } = default!;

    [Text(128)]
    public string RoutingKey { get; set; } = default!;

    [Text(65535)]
    public string Payload { get; set; } = default!;

    [PreciseTimestamp]
    public DateTime OccurredAt { get; set; }

    [PreciseTimestamp]
    public DateTime? PublishedAt { get; set; }

    [PreciseTimestamp]
    public DateTime? LeasedUntil { get; set; }

    [Integer]
    public int Attempts { get; set; }

    [Text(500)]
    public string? LastError { get; set; }
}
