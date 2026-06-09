using System.ComponentModel.DataAnnotations;

namespace Backend.Options;

public sealed class SubscriptionGrpcOptions
{
    [Required]
    public string AuthUrl { get; set; } = string.Empty;

    [Required]
    public string Secret { get; set; } = string.Empty;
}
