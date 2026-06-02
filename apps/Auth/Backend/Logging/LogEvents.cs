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

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information,
        Message = "Login succeeded for user {UserId} on tenant {TenantId}")]
    public static partial void LoginSucceeded(ILogger logger, Guid userId, Guid tenantId);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Warning,
        Message = "Login failed: user {UserName} not found")]
    public static partial void LoginFailedUserNotFound(ILogger logger, string userName);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Warning,
        Message = "Login failed: invalid password for user {UserName}")]
    public static partial void LoginFailedInvalidPassword(ILogger logger, string userName);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information,
        Message = "User {UserName} registered as {Role} on tenant {TenantId}")]
    public static partial void UserRegistered(ILogger logger, string userName, string role, Guid tenantId);

    [LoggerMessage(EventId = 3005, Level = LogLevel.Warning,
        Message = "Registration rejected: duplicate username {UserName} for role {Role} on tenant {TenantId}")]
    public static partial void UserRegistrationDuplicateName(ILogger logger, string userName, string role, Guid tenantId);

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information,
        Message = "Tenant {TenantId} subscription updated to level {Level} via gRPC")]
    public static partial void TenantSubscriptionUpdated(ILogger logger, Guid tenantId, int level);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Warning,
        Message = "Tenant subscription gRPC call rejected: invalid service secret")]
    public static partial void TenantSubscriptionUnauthorized(ILogger logger);
}
