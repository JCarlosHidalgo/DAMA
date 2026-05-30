using System.Diagnostics;

using Backend.Events;
using Backend.Options;
using Backend.Results.Events;
using Backend.Services.Abstract.Events;
using Backend.Transporters.Config;
using Backend.Workers.Infrastructure;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;

namespace Backend.Workers.Events;

public sealed class StudentRegisteredConsumer : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqTopologyDeclarer _topologyDeclarer;
    private readonly RabbitMqMessageDispatcher<StudentRegisteredEvent> _messageDispatcher;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<StudentRegisteredConsumer> _logger;

    public StudentRegisteredConsumer(
        RabbitMqConnectionFactory connectionFactory,
        RabbitMqTopologyDeclarer topologyDeclarer,
        RabbitMqMessageDispatcher<StudentRegisteredEvent> messageDispatcher,
        IOptions<RabbitMqOptions> options,
        ILogger<StudentRegisteredConsumer> logger)
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
            QueueName: _options.StudentRegisteredQueueName,
            RoutingKey: _options.StudentRegisteredRoutingKey,
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
                _logger.LogError(consumerException, "StudentRegisteredConsumer connection error");

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
                "StudentRegisteredConsumer subscribed to {QueueName}",
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
        StudentRegisteredEvent studentRegisteredEvent,
        CancellationToken cancellationToken)
    {
        IStudentRegisteredHandler handler =
            scopedServiceProvider.GetRequiredService<IStudentRegisteredHandler>();
        StudentRegisteredOutcome outcome = await handler.HandleAsync(studentRegisteredEvent, cancellationToken);
        return outcome switch
        {
            StudentRegisteredOutcome.RemainCreated => true,
            StudentRegisteredOutcome.AlreadyProcessed => true,
            StudentRegisteredOutcome.Failed => false,
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
