using System.Text.Json;

using Backend.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Backend.Workers.Infrastructure;

public sealed class RabbitMqMessageDispatcher<TEvent> where TEvent : class
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqMessageDispatcher<TEvent>> _logger;

    public RabbitMqMessageDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqMessageDispatcher<TEvent>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunAsync(
        IChannel channel,
        IConnection connection,
        string queueName,
        Func<IServiceProvider, TEvent, CancellationToken, Task<bool>> handle,
        Predicate<TEvent>? isPoisonMessage,
        CancellationToken cancellationToken)
    {
        TaskCompletionSource completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration cancellationRegistration =
            cancellationToken.Register(() => completionSource.TrySetResult());

        channel.ChannelShutdownAsync += (_, shutdownEventArgs) =>
        {
            LogEvents.RabbitMqChannelShutdown(_logger, shutdownEventArgs.ReplyText);
            completionSource.TrySetResult();
            return Task.CompletedTask;
        };
        connection.ConnectionShutdownAsync += (_, shutdownEventArgs) =>
        {
            LogEvents.RabbitMqConnectionShutdown(_logger, shutdownEventArgs.ReplyText);
            completionSource.TrySetResult();
            return Task.CompletedTask;
        };

        AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, deliveryEventArgs) =>
            HandleDeliveryAsync(channel, deliveryEventArgs, handle, isPoisonMessage, cancellationToken);

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        await completionSource.Task;
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs deliveryEventArgs,
        Func<IServiceProvider, TEvent, CancellationToken, Task<bool>> handle,
        Predicate<TEvent>? isPoisonMessage,
        CancellationToken cancellationToken)
    {
        using IDisposable? deliveryScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["EventId"] = deliveryEventArgs.BasicProperties.MessageId ?? string.Empty,
            ["EventType"] = deliveryEventArgs.BasicProperties.Type ?? typeof(TEvent).Name
        });

        TEvent? deserializedEvent;
        try
        {
            deserializedEvent = JsonSerializer.Deserialize<TEvent>(deliveryEventArgs.Body.Span, SerializerOptions);
        }
        catch (Exception deserializationException)
        {
            LogEvents.BadPayloadDropped(
                _logger,
                deserializationException,
                deliveryEventArgs.RoutingKey,
                deliveryEventArgs.DeliveryTag);
            await channel.BasicAckAsync(deliveryEventArgs.DeliveryTag, multiple: false);
            return;
        }

        if (deserializedEvent is null || (isPoisonMessage is not null && isPoisonMessage(deserializedEvent)))
        {
            LogEvents.InvalidEventDropped(_logger, typeof(TEvent).Name, deliveryEventArgs.DeliveryTag);
            await channel.BasicAckAsync(deliveryEventArgs.DeliveryTag, multiple: false);
            return;
        }

        bool wasHandled = await DispatchToHandlerAsync(handle, deserializedEvent, deliveryEventArgs, cancellationToken);
        await AcknowledgeAsync(channel, deliveryEventArgs.DeliveryTag, wasHandled);
    }

    private async Task<bool> DispatchToHandlerAsync(
        Func<IServiceProvider, TEvent, CancellationToken, Task<bool>> handle,
        TEvent deserializedEvent,
        BasicDeliverEventArgs deliveryEventArgs,
        CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            return await handle(scope.ServiceProvider, deserializedEvent, cancellationToken);
        }
        catch (Exception handlerException)
        {
            LogEvents.HandlerThrew(_logger, handlerException, deliveryEventArgs.DeliveryTag, typeof(TEvent).Name);
            return false;
        }
    }

    private static async Task AcknowledgeAsync(IChannel channel, ulong deliveryTag, bool wasHandled)
    {
        if (wasHandled)
        {
            await channel.BasicAckAsync(deliveryTag, multiple: false);
        }
        else
        {
            await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true);
        }
    }
}
