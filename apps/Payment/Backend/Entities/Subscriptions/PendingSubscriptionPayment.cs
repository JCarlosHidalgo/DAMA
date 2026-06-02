using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Subscriptions;

public class PendingSubscriptionPayment : IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Identifier]
    public Guid TenantId { get; set; }

    [Integer]
    public int Level { get; set; }

    [Integer]
    public int Cost { get; set; }

    [Text(512)]
    public string? QrImageUrl { get; set; }

    [Timestamp]
    public DateTime CreatedAt { get; set; }

    [PreciseTimestamp]
    public DateTime ExpiresAt { get; set; }
}
