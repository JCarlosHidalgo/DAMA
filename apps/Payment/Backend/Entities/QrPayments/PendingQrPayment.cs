using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.QrPayments;

public class PendingQrPayment : IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Identifier]
    public Guid TenantId { get; set; }

    [Identifier]
    public Guid StudentId { get; set; }

    [Identifier]
    public Guid TemplateId { get; set; }

    [Integer]
    public int ClassQuantity { get; set; }

    [Integer]
    public int Cost { get; set; }

    [Text(3)]
    public string Currency { get; set; } = "BOB";

    [Text(512)]
    public string? QrImageUrl { get; set; }

    [Timestamp]
    public DateTime CreatedAt { get; set; }

    [PreciseTimestamp]
    public DateTime ExpiresAt { get; set; }
}
