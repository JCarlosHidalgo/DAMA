using System.ComponentModel.DataAnnotations;

namespace Backend.Options;

public sealed class SubscriptionGrpcOptions
{
    [Required]
    public string Secret { get; set; } = string.Empty;
}
