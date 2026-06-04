using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Subscriptions;

public class SubscriptionPlan : IEntity
{
    [Identifier]
    public int Level { get; set; }

    [Integer]
    public int Price { get; set; }

    [Text(3)]
    public string Currency { get; set; } = "BOB";

    [Integer]
    public int DurationAmount { get; set; }

    [Text(8)]
    public string DurationUnit { get; set; } = nameof(SubscriptionDurationUnit.Month);

    [Timestamp]
    public DateTime UpdatedAt { get; set; }
}
