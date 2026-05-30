using System.ComponentModel.DataAnnotations;

namespace Backend.Options;

public sealed class RabbitMqOptions
{
    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    [Required]
    public string User { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
