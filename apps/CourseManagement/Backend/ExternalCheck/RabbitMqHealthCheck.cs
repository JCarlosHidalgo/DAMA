using Backend.Options;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;

namespace Backend.ExternalCheck;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqOptions _options;

    public RabbitMqHealthCheck(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ConnectionFactory connectionFactory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.User,
                Password = _options.Password
            };

            await using IConnection connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

            return HealthCheckResult.Healthy("RabbitMQ connection succeeded.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ connection failed.", exception);
        }
    }
}
