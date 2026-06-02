namespace Backend.Logging;

public static partial class LogEvents
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning,
        Message = "RabbitMQ channel shutdown: {Reason}")]
    public static partial void RabbitMqChannelShutdown(ILogger logger, string reason);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "RabbitMQ connection shutdown: {Reason}")]
    public static partial void RabbitMqConnectionShutdown(ILogger logger, string reason);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error,
        Message = "Bad payload on {RoutingKey} (DeliveryTag {DeliveryTag}); dropping")]
    public static partial void BadPayloadDropped(ILogger logger, Exception exception, string routingKey, ulong deliveryTag);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Error,
        Message = "Invalid {EventType} (DeliveryTag {DeliveryTag}); dropping")]
    public static partial void InvalidEventDropped(ILogger logger, string eventType, ulong deliveryTag);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Error,
        Message = "Handler threw for delivery {DeliveryTag} ({EventType})")]
    public static partial void HandlerThrew(ILogger logger, Exception exception, ulong deliveryTag, string eventType);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Error,
        Message = "{ConsumerName} connection error")]
    public static partial void ConsumerConnectionError(ILogger logger, Exception exception, string consumerName);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Information,
        Message = "{ConsumerName} subscribed to {QueueName}")]
    public static partial void ConsumerSubscribed(ILogger logger, string consumerName, string queueName);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Warning,
        Message = "Error disposing RabbitMQ resource")]
    public static partial void RabbitMqResourceDisposeFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 6001, Level = LogLevel.Error,
        Message = "Handling {EventType} failed for event {EventId}")]
    public static partial void EventHandlerFailed(ILogger logger, Exception exception, string eventType, Guid eventId);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning,
        Message = "Tenant timezone '{TimezoneId}' is not usable ({ExceptionType}); falling back to UTC")]
    public static partial void TenantTimezoneUnusableFallback(ILogger logger, Exception exception, string timezoneId, string exceptionType);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning,
        Message = "Tenant timezone '{TimezoneId}' is invalid; rejecting attendance marking")]
    public static partial void TenantTimezoneInvalidRejected(ILogger logger, string timezoneId);
}
