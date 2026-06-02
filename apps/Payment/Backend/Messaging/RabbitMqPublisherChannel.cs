using Backend.Logging;
using Backend.Options;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;

namespace Backend.Messaging;

public sealed class RabbitMqPublisherChannel : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisherChannel> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisherChannel(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisherChannel> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IChannel> EnsureChannelAsync(Func<IChannel, CancellationToken, Task> declareTopology, CancellationToken cancellationToken)
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
            IChannel channel = await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);
            await declareTopology(channel, cancellationToken);
            _channel = channel;

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
        LogEvents.RabbitMqConnectionEstablished(_logger, connectionFactory.HostName, connectionFactory.Port);
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
