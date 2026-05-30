using System.Text;

using Backend.Entities;
using Backend.Services.Abstract;

using RabbitMQ.Client;

namespace Backend.Messaging;

public sealed class RabbitMqDomainEventPublisher : IOutboxPublisher<OutboxEvent>
{
    private const string ExchangeName = "dama.events";

    private readonly RabbitMqPublisherChannel _publisherChannel;

    public RabbitMqDomainEventPublisher(RabbitMqPublisherChannel publisherChannel)
    {
        _publisherChannel = publisherChannel;
    }

    public async Task PublishAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        IChannel channel = await _publisherChannel.EnsureChannelAsync(DeclareTopologyAsync, cancellationToken);

        var basicProperties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = outboxEvent.Id.ToString(),
            Type = outboxEvent.EventType
        };

        ReadOnlyMemory<byte> body = Encoding.UTF8.GetBytes(outboxEvent.Payload);

        await channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: outboxEvent.RoutingKey,
            mandatory: true,
            basicProperties: basicProperties,
            body: body,
            cancellationToken: cancellationToken);
    }

    private static Task DeclareTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        return channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
    }
}
