namespace Backend.Logging;

public static partial class LogEvents
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "RabbitMQ connection established to {Host}:{Port}")]
    public static partial void RabbitMqConnectionEstablished(ILogger logger, string host, int port);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "RabbitMQ channel shutdown: {Reason}")]
    public static partial void RabbitMqChannelShutdown(ILogger logger, string reason);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning,
        Message = "RabbitMQ connection shutdown: {Reason}")]
    public static partial void RabbitMqConnectionShutdown(ILogger logger, string reason);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Error,
        Message = "Bad payload on {RoutingKey} (DeliveryTag {DeliveryTag}); dropping")]
    public static partial void BadPayloadDropped(ILogger logger, Exception exception, string routingKey, ulong deliveryTag);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Error,
        Message = "Invalid {EventType} (DeliveryTag {DeliveryTag}); dropping")]
    public static partial void InvalidEventDropped(ILogger logger, string eventType, ulong deliveryTag);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Error,
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

    [LoggerMessage(EventId = 2001, Level = LogLevel.Error,
        Message = "OutboxRelayWorker<{EventType}> loop error")]
    public static partial void OutboxRelayLoopError(ILogger logger, Exception exception, string eventType);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
        Message = "Outbox publish failed for event {EventId}")]
    public static partial void OutboxPublishFailed(ILogger logger, Exception exception, Guid eventId);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information,
        Message = "OutboxJanitor<{EventType}> deleted {DeletedCount} published rows older than {RetentionAge}")]
    public static partial void OutboxJanitorDeletedPublished(ILogger logger, string eventType, int deletedCount, TimeSpan retentionAge);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Error,
        Message = "OutboxJanitor<{EventType}> sweep error")]
    public static partial void OutboxJanitorSweepError(ILogger logger, Exception exception, string eventType);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information,
        Message = "ProcessedEventsJanitor deleted {DeletedCount} processed events older than {RetentionAge}")]
    public static partial void ProcessedEventsJanitorDeleted(ILogger logger, int deletedCount, TimeSpan retentionAge);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Error,
        Message = "ProcessedEventsJanitor sweep error")]
    public static partial void ProcessedEventsJanitorSweepError(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Error,
        Message = "PaymentCallbackWorker loop error")]
    public static partial void PaymentCallbackWorkerLoopError(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2008, Level = LogLevel.Warning,
        Message = "Payment callback {CallbackId} failed (attempt {AttemptCount})")]
    public static partial void PaymentCallbackFailed(ILogger logger, Exception exception, Guid callbackId, int attemptCount);

    [LoggerMessage(EventId = 2009, Level = LogLevel.Error,
        Message = "TodotixOutboxWorker loop error")]
    public static partial void TodotixOutboxWorkerLoopError(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Warning,
        Message = "Payment outbox row {OutboxRowId} failed (attempt {AttemptCount})")]
    public static partial void PaymentOutboxRowFailed(ILogger logger, Exception exception, Guid outboxRowId, int attemptCount);

    [LoggerMessage(EventId = 5001, Level = LogLevel.Warning,
        Message = "Rejected Todotix callback with invalid signature for transaction {TransactionId}")]
    public static partial void TodotixCallbackInvalidSignature(ILogger logger, Guid transactionId);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Warning,
        Message = "Todotix ConsultDebt {DebtId} body did not deserialize as ConsultDebtResponse")]
    public static partial void TodotixConsultDebtDeserializationFailed(ILogger logger, Exception exception, Guid debtId);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Warning,
        Message = "Todotix ConsultDebt {DebtId} returned Unpaid. Error={Error} Existente={Existente} Pagado={Pagado} PagoAnulado={PagoAnulado}")]
    public static partial void TodotixConsultDebtUnpaid(ILogger logger, Guid debtId, int error, int existente, bool? pagado, bool? pagoAnulado);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Warning,
        Message = "Credential test failed for tenant {TenantId}: Todotix returned Error={Error} Mensaje={Mensaje}")]
    public static partial void TodotixCredentialTestFailed(ILogger logger, Guid tenantId, int error, string? mensaje);

    [LoggerMessage(EventId = 5005, Level = LogLevel.Warning,
        Message = "Credential test failed for tenant {TenantId}: HTTP error calling Todotix")]
    public static partial void TodotixCredentialTestHttpError(ILogger logger, Exception exception, Guid tenantId);

    [LoggerMessage(EventId = 6001, Level = LogLevel.Error,
        Message = "Handling {EventType} failed for event {EventId}")]
    public static partial void EventHandlerFailed(ILogger logger, Exception exception, string eventType, Guid eventId);
}
