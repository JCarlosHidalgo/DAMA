using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Subscriptions;

public class SuccessSubscriptionPayment : IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Identifier]
    public Guid TenantId { get; set; }

    [Integer]
    public int Level { get; set; }

    [Integer]
    public int Cost { get; set; }

    [Timestamp]
    public DateTime PaidAt { get; set; }
}
