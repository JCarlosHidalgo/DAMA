namespace Backend.Transporters.Config;

public sealed record RabbitMqTopologyDescriptor(
    string ExchangeName,
    string QueueName,
    string RoutingKey,
    ushort PrefetchCount);
