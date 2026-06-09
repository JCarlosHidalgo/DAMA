using System.ComponentModel.DataAnnotations;

namespace Backend.Options;

public sealed class RabbitMqOptions
{
    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; }

    [Required]
    public string User { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string DelayedExchangeName { get; set; } = "dama.delayed";

    public string QueueName { get; set; } = "payment.debt-expired";

    public string RoutingKey { get; set; } = "debt.expired";

    public ushort PrefetchCount { get; set; } = 10;

    public int ReconnectDelaySeconds { get; set; } = 5;
}
