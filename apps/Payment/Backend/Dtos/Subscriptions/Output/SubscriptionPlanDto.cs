namespace Backend.Dtos.Subscriptions.Output;

public sealed class SubscriptionPlanDto
{
    public int Level { get; set; }

    public int Price { get; set; }

    public string Currency { get; set; } = "BOB";

    public int DurationAmount { get; set; }

    public string DurationUnit { get; set; } = "Month";
}
