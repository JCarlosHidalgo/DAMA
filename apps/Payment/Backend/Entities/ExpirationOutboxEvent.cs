using DAMA.Software.MySqlOutbox;

using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities;

public sealed class ExpirationOutboxEvent : IOutboxEvent, IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Identifier]
    public Guid AggregateId { get; set; }

    [Text(128)]
    public string EventType { get; set; } = string.Empty;

    [Text(128)]
    public string RoutingKey { get; set; } = string.Empty;

    [Text(65535)]
    public string Payload { get; set; } = string.Empty;

    [PreciseTimestamp]
    public DateTime OccurredAt { get; set; }

    [PreciseTimestamp]
    public DateTime AvailableAt { get; set; }

    [Integer]
    public int Attempts { get; set; }

    [Text(500)]
    public string? LastError { get; set; }
}
