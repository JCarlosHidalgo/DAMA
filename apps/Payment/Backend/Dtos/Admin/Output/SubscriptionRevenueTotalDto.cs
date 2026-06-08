namespace Backend.Dtos.Admin.Output;

public class SubscriptionRevenueTotalDto
{
    public int TotalRevenue { get; set; }

    public int PaymentCount { get; set; }

    public string Currency { get; set; } = "BOB";
}
