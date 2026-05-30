using System.Diagnostics;

using Backend.Events;
using Backend.Options;
using Backend.Results.QrPayments;
using Backend.Services.Abstract.Events;
using Backend.Transporters.Config;
using Backend.Workers.Infrastructure;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;

namespace Backend.Workers.Events;

public sealed class DebtExpiredConsumer : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqTopologyDeclarer _topologyDeclarer;
    private readonly RabbitMqMessageDispatcher<DebtExpiredEvent> _messageDispatcher;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<DebtExpiredConsumer> _logger;

    public DebtExpiredConsumer(
        RabbitMqConnectionFactory connectionFactory,
        RabbitMqTopologyDeclarer topologyDeclarer,
        RabbitMqMessageDispatcher<DebtExpiredEvent> messageDispatcher,
        IOptions<RabbitMqOptions> options,
        ILogger<DebtExpiredConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _topologyDeclarer = topologyDeclarer;
        _messageDispatcher = messageDispatcher;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        TimeSpan reconnectDelay = TimeSpan.FromSeconds(_options.ReconnectDelaySeconds);
        RabbitMqTopologyDescriptor topologyDescriptor = new RabbitMqTopologyDescriptor(
            DelayedExchangeName: _options.DelayedExchangeName,
            QueueName: _options.QueueName,
            RoutingKey: _options.RoutingKey,
            PrefetchCount: _options.PrefetchCount);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilDisconnectedAsync(topologyDescriptor, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception consumerException)
            {
                _logger.LogError(consumerException, "DebtExpiredConsumer connection error");

                try
                {
                    await Task.Delay(reconnectDelay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private async Task ConsumeUntilDisconnectedAsync(
        RabbitMqTopologyDescriptor topologyDescriptor,
        CancellationToken cancellationToken)
    {
        IConnection? connection = null;
        IChannel? channel = null;
        try
        {
            (connection, channel) = await _connectionFactory.OpenAsync(cancellationToken);
            await _topologyDeclarer.DeclareAsync(channel, topologyDescriptor, cancellationToken);

            _logger.LogInformation(
                "DebtExpiredConsumer subscribed to {QueueName}",
                topologyDescriptor.QueueName);

            await _messageDispatcher.RunAsync(
                channel,
                connection,
                topologyDescriptor.QueueName,
                handle: ResolveAndHandleAsync,
                isPoisonMessage: deserializedEvent => deserializedEvent.EventId == Guid.Empty,
                cancellationToken: cancellationToken);
        }
        finally
        {
            await DisposeQuietlyAsync(channel);
            await DisposeQuietlyAsync(connection);
        }
    }

    private static async Task<bool> ResolveAndHandleAsync(
        IServiceProvider scopedServiceProvider,
        DebtExpiredEvent debtExpiredEvent,
        CancellationToken cancellationToken)
    {
        IDebtExpiredHandler handler = scopedServiceProvider.GetRequiredService<IDebtExpiredHandler>();
        HandleDebtExpiredOutcome outcome = await handler.HandleAsync(debtExpiredEvent, cancellationToken);
        return outcome switch
        {
            HandleDebtExpiredOutcome.Processed => true,
            HandleDebtExpiredOutcome.AlreadyProcessed => true,
            HandleDebtExpiredOutcome.PendingMissing => true,
            HandleDebtExpiredOutcome.Failed => false,
            _ => throw new UnreachableException()
        };
    }

    private async Task DisposeQuietlyAsync(IAsyncDisposable? resource)
    {
        if (resource is null)
        {
            return;
        }

        try
        {
            await resource.DisposeAsync();
        }
        catch (Exception disposeException)
        {
            _logger.LogWarning(disposeException, "Error disposing RabbitMQ resource");
        }
    }
}
