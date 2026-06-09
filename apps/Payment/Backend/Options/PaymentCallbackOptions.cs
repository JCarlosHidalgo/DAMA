using System.ComponentModel.DataAnnotations;

namespace Backend.Options;

public sealed class PaymentCallbackOptions
{
    [Required]
    public string Secret { get; set; } = string.Empty;
}
