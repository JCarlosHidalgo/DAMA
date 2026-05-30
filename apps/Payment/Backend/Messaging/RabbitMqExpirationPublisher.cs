using System.Text;

using Backend.Entities;
using Backend.Options;
using Backend.Services.Abstract;
using Backend.Transporters.Config;
using Backend.Workers.Infrastructure;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;

namespace Backend.Messaging;

public sealed class RabbitMqExpirationPublisher : IOutboxPublisher<ExpirationOutboxEvent>
{
    private readonly RabbitMqOptions _options;
    private readonly RabbitMqTopologyDeclarer _topologyDeclarer;
    private readonly RabbitMqPublisherChannel _publisherChannel;

    public RabbitMqExpirationPublisher(
        IOptions<RabbitMqOptions> options,
        RabbitMqTopologyDeclarer topologyDeclarer,
        RabbitMqPublisherChannel publisherChannel)
    {
        _options = options.Value;
        _topologyDeclarer = topologyDeclarer;
        _publisherChannel = publisherChannel;
    }

    public async Task PublishAsync(ExpirationOutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        IChannel channel = await _publisherChannel.EnsureChannelAsync(DeclareTopologyAsync, cancellationToken);

        var basicProperties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = outboxEvent.Id.ToString(),
            Type = outboxEvent.EventType,
            Headers = new Dictionary<string, object?>
            {
                ["x-delay"] = ComputeDelayMilliseconds(outboxEvent.AvailableAt)
            }
        };

        ReadOnlyMemory<byte> body = Encoding.UTF8.GetBytes(outboxEvent.Payload);

        await channel.BasicPublishAsync(
            exchange: _options.DelayedExchangeName,
            routingKey: outboxEvent.RoutingKey,
            mandatory: true,
            basicProperties: basicProperties,
            body: body,
            cancellationToken: cancellationToken);
    }

    private Task DeclareTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        RabbitMqTopologyDescriptor topologyDescriptor = new RabbitMqTopologyDescriptor(
            DelayedExchangeName: _options.DelayedExchangeName,
            QueueName: _options.QueueName,
            RoutingKey: _options.RoutingKey,
            PrefetchCount: _options.PrefetchCount);
        return _topologyDeclarer.DeclareAsync(channel, topologyDescriptor, cancellationToken);
    }

    private static int ComputeDelayMilliseconds(DateTime availableAtUtc)
    {
        double remainingMilliseconds = (availableAtUtc - DateTime.UtcNow).TotalMilliseconds;
        if (remainingMilliseconds <= 0)
        {
            return 0;
        }

        return remainingMilliseconds >= int.MaxValue ? int.MaxValue : (int)remainingMilliseconds;
    }
}
