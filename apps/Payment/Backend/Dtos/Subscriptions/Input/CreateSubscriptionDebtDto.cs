namespace Backend.Dtos.Subscriptions.Input;

public sealed class CreateSubscriptionDebtDto
{
    public int Level { get; set; }

    public string Method { get; set; } = "QR";

    public string? Email { get; set; }
}
