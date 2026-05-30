using Backend.Options;

namespace Backend.Modules;

public sealed class OptionsModule : IServiceModule
{
    public int Order => 10;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TodotixOptions>(options =>
        {
            options.ApplicationKey = configuration["TODOTIX_APPKEY"]
                ?? throw new InvalidOperationException("TODOTIX_APPKEY not set.");
            options.CallbackUrl = configuration["TODOTIX_CALLBACK_URL"]
                ?? throw new InvalidOperationException("TODOTIX_CALLBACK_URL not set.");
        });

        services.Configure<PaymentCallbackOptions>(options =>
        {
            options.Secret = configuration["PAYMENT_CALLBACK_SECRET"]
                ?? throw new InvalidOperationException("PAYMENT_CALLBACK_SECRET not set.");
        });

        services.Configure<RabbitMqOptions>(options =>
        {
            options.Host = configuration["RABBITMQ_HOST"]
                ?? throw new InvalidOperationException("RABBITMQ_HOST not set.");
            string rawPort = configuration["RABBITMQ_PORT"]
                ?? throw new InvalidOperationException("RABBITMQ_PORT not set.");
            if (!int.TryParse(rawPort, out int parsedPort))
            {
                throw new InvalidOperationException("RABBITMQ_PORT is not a valid integer.");
            }
            options.Port = parsedPort;
            options.User = configuration["RABBITMQ_USER"]
                ?? throw new InvalidOperationException("RABBITMQ_USER not set.");
            options.Password = configuration["RABBITMQ_PASSWORD"]
                ?? throw new InvalidOperationException("RABBITMQ_PASSWORD not set.");
        });
    }
}
