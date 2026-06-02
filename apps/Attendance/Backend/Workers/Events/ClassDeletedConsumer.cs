using System.Diagnostics;

using Backend.Events;
using Backend.Logging;
using Backend.Options;
using Backend.Results.Events;
using Backend.Services.Abstract.Events;
using Backend.Transporters.Config;
using Backend.Workers.Infrastructure;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;

namespace Backend.Workers.Events;

public sealed class ClassDeletedConsumer : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqTopologyDeclarer _topologyDeclarer;
    private readonly RabbitMqMessageDispatcher<ClassDeletedEvent> _messageDispatcher;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<ClassDeletedConsumer> _logger;

    public ClassDeletedConsumer(
        RabbitMqConnectionFactory connectionFactory,
        RabbitMqTopologyDeclarer topologyDeclarer,
        RabbitMqMessageDispatcher<ClassDeletedEvent> messageDispatcher,
        IOptions<RabbitMqOptions> options,
        ILogger<ClassDeletedConsumer> logger)
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
            ExchangeName: _options.ExchangeName,
            QueueName: _options.ClassDeletedQueueName,
            RoutingKey: _options.ClassDeletedRoutingKey,
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
                LogEvents.ConsumerConnectionError(_logger, consumerException, "ClassDeletedConsumer");

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

            LogEvents.ConsumerSubscribed(_logger, "ClassDeletedConsumer", topologyDescriptor.QueueName);

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
        ClassDeletedEvent classDeletedEvent,
        CancellationToken cancellationToken)
    {
        IClassDeletedHandler handler =
            scopedServiceProvider.GetRequiredService<IClassDeletedHandler>();
        ClassDeletedOutcome outcome = await handler.HandleAsync(classDeletedEvent, cancellationToken);
        return outcome switch
        {
            ClassDeletedOutcome.AttendancesDeleted => true,
            ClassDeletedOutcome.AlreadyProcessed => true,
            ClassDeletedOutcome.Failed => false,
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
            LogEvents.RabbitMqResourceDisposeFailed(_logger, disposeException);
        }
    }
}
