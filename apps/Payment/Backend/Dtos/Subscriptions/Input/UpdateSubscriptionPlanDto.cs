namespace Backend.Dtos.Subscriptions.Input;

public sealed class UpdateSubscriptionPlanDto
{
    public int Price { get; set; }

    public int DurationAmount { get; set; }

    public string DurationUnit { get; set; } = "Month";
}
