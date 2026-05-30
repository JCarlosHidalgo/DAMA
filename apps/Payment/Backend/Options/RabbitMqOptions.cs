namespace Backend.Options;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string DelayedExchangeName { get; set; } = "dama.delayed";

    public string QueueName { get; set; } = "payment.debt-expired";

    public string RoutingKey { get; set; } = "debt.expired";

    public ushort PrefetchCount { get; set; } = 10;

    public int ReconnectDelaySeconds { get; set; } = 5;
}
