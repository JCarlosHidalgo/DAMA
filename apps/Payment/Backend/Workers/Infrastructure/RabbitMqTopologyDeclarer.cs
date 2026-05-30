using Backend.Transporters.Config;

using RabbitMQ.Client;

namespace Backend.Workers.Infrastructure;

public sealed class RabbitMqTopologyDeclarer
{
    private const string DelayedMessageExchangeType = "x-delayed-message";

    public async Task DeclareAsync(
        IChannel channel,
        RabbitMqTopologyDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        Dictionary<string, object?> exchangeArguments = new Dictionary<string, object?>
        {
            ["x-delayed-type"] = ExchangeType.Topic
        };

        await channel.ExchangeDeclareAsync(
            exchange: descriptor.DelayedExchangeName,
            type: DelayedMessageExchangeType,
            durable: true,
            autoDelete: false,
            arguments: exchangeArguments,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: descriptor.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: descriptor.QueueName,
            exchange: descriptor.DelayedExchangeName,
            routingKey: descriptor.RoutingKey,
            cancellationToken: cancellationToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: descriptor.PrefetchCount,
            global: false,
            cancellationToken: cancellationToken);
    }
}
