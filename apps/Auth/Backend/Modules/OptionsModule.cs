using Backend.Messaging;
using Backend.Options;

namespace Backend.Modules;

public sealed class OptionsModule : IServiceModule
{
    public int Order => 10;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RabbitMqOptions>()
            .Configure(rabbitMqOptions =>
            {
                rabbitMqOptions.Host = configuration["RABBITMQ_HOST"]
                    ?? throw new InvalidOperationException("RABBITMQ_HOST not set.");
                string rawPort = configuration["RABBITMQ_PORT"]
                    ?? throw new InvalidOperationException("RABBITMQ_PORT not set.");
                if (!int.TryParse(rawPort, out int parsedPort))
                {
                    throw new InvalidOperationException("RABBITMQ_PORT is not a valid integer.");
                }
                rabbitMqOptions.Port = parsedPort;
                rabbitMqOptions.User = configuration["RABBITMQ_USER"]
                    ?? throw new InvalidOperationException("RABBITMQ_USER not set.");
                rabbitMqOptions.Password = configuration["RABBITMQ_PASSWORD"]
                    ?? throw new InvalidOperationException("RABBITMQ_PASSWORD not set.");
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
