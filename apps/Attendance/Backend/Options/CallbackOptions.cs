using System.ComponentModel.DataAnnotations;

namespace Backend.Options;

public sealed class CallbackOptions
{
    [Required]
    public string? Secret { get; set; }
}
