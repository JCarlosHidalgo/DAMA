using Backend.Transporters.Config;

using RabbitMQ.Client;

namespace Backend.Workers.Infrastructure;

public sealed class RabbitMqTopologyDeclarer
{
    public async Task DeclareAsync(
        IChannel channel,
        RabbitMqTopologyDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: descriptor.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: descriptor.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: descriptor.QueueName,
            exchange: descriptor.ExchangeName,
            routingKey: descriptor.RoutingKey,
            cancellationToken: cancellationToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: descriptor.PrefetchCount,
            global: false,
            cancellationToken: cancellationToken);
    }
}
