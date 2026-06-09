using System.ComponentModel.DataAnnotations;

namespace Backend.Options;

public sealed class TodotixOptions
{
    [Required]
    public string CallbackUrl { get; set; } = string.Empty;

    public string PlatformAppKey { get; set; } = string.Empty;
}
