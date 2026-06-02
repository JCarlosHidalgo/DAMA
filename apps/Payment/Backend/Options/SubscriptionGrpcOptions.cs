namespace Backend.Options;

public sealed class SubscriptionGrpcOptions
{
    public string AuthUrl { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;
}
