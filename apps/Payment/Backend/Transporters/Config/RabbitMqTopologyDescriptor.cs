namespace Backend.Transporters.Config;

public sealed record RabbitMqTopologyDescriptor(
    string DelayedExchangeName,
    string QueueName,
    string RoutingKey,
    ushort PrefetchCount);
