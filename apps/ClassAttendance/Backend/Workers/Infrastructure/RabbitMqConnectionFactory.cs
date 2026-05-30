using Backend.Options;

using Microsoft.Extensions.Options;

using RabbitMQ.Client;

namespace Backend.Workers.Infrastructure;

public sealed class RabbitMqConnectionFactory
{
    private readonly RabbitMqOptions _options;

    public RabbitMqConnectionFactory(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task<(IConnection Connection, IChannel Channel)> OpenAsync(CancellationToken cancellationToken)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.User,
            Password = _options.Password
        };

        IConnection connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        IChannel channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        return (connection, channel);
    }
}
