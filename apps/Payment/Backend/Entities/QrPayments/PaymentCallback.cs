using DAMA.Software.MySqlOutbox;

using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.QrPayments;

public class PaymentCallback : IOutboxEvent, IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Integer]
    public int Error { get; set; }

    [Integer]
    public int CancelOrder { get; set; }

    [PreciseTimestamp]
    public DateTime OccurredAt { get; set; }

    [Integer]
    public int Attempts { get; set; }

    [Text(500)]
    public string? LastError { get; set; }

    [Text(16)]
    public string Status { get; set; } = "Pending";
}
