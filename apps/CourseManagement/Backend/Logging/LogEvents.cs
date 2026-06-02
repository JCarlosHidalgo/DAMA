namespace Backend.Logging;

public static partial class LogEvents
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "RabbitMQ connection established to {Host}:{Port}")]
    public static partial void RabbitMqConnectionEstablished(ILogger logger, string host, int port);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Error,
        Message = "OutboxPublisher loop error")]
    public static partial void OutboxPublisherLoopError(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
        Message = "Outbox publish failed for event {EventId}")]
    public static partial void OutboxPublishFailed(ILogger logger, Exception exception, Guid eventId);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information,
        Message = "OutboxJanitor deleted {DeletedCount} published events older than {RetentionAge}")]
    public static partial void OutboxJanitorDeletedPublished(ILogger logger, int deletedCount, TimeSpan retentionAge);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Error,
        Message = "OutboxJanitor sweep error")]
    public static partial void OutboxJanitorSweepError(ILogger logger, Exception exception);
}
