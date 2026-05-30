using DAMA.Software.MySqlOutbox;

using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Todotix;

public class TodotixOutboxEvent : IOutboxEvent, IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Identifier]
    public Guid PendingId { get; set; }

    [Identifier]
    public Guid TenantId { get; set; }

    [Text(65535)]
    public string PayloadJson { get; set; } = string.Empty;

    [PreciseTimestamp]
    public DateTime OccurredAt { get; set; }

    [Integer]
    public int Attempts { get; set; }

    [Text(500)]
    public string? LastError { get; set; }

    [Text(16)]
    public string Status { get; set; } = "Pending";
}
