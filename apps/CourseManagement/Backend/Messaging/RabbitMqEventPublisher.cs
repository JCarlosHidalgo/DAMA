using System.Text;

using Backend.Entities;
using Backend.Options;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;

namespace Backend.Messaging;

public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private const string ExchangeName = "dama.events";

    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqEventPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        IChannel channel = await EnsureChannelAsync(cancellationToken);

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

    private async Task<IChannel> EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            await DiscardStaleConnectionStateAsync();
            await EnsureConnectionAsync(cancellationToken);
            _channel = await CreateDeclaredChannelAsync(cancellationToken);

            return _channel;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task DiscardStaleConnectionStateAsync()
    {
        if (_channel is not null)
        {
            await TryDisposeAsync(_channel);
            _channel = null;
        }
        if (_connection is { IsOpen: false })
        {
            await TryDisposeAsync(_connection);
            _connection = null;
        }
    }

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            return;
        }

        ConnectionFactory connectionFactory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.User,
            Password = _options.Password
        };
        _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        _logger.LogInformation("RabbitMQ connection established to {Host}:{Port}", connectionFactory.HostName, connectionFactory.Port);
    }

    private async Task<IChannel> CreateDeclaredChannelAsync(CancellationToken cancellationToken)
    {
        IChannel channel = await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        return channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await TryDisposeAsync(_channel);
            _channel = null;
        }
        if (_connection is not null)
        {
            await TryDisposeAsync(_connection);
            _connection = null;
        }
        _initLock.Dispose();
    }

    private static async Task TryDisposeAsync(IAsyncDisposable disposable)
    {
        try
        {
            await disposable.DisposeAsync();
        }
        catch
        {
        }
    }
}
