using Backend.Options;
using Backend.Security;

namespace Backend.Modules;

public sealed class OptionsModule : IServiceModule
{
    public int Order => 10;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TodotixOptions>()
            .Configure(options =>
            {
                options.CallbackUrl = configuration["TODOTIX_CALLBACK_URL"]
                    ?? throw new InvalidOperationException("TODOTIX_CALLBACK_URL not set.");
                options.PlatformAppKey = configuration["TODOTIX_APPKEY"] ?? string.Empty;
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SubscriptionGrpcOptions>()
            .Configure(options =>
            {
                options.AuthUrl = configuration["SUBSCRIPTION_GRPC_AUTH_URL"]
                    ?? throw new InvalidOperationException("SUBSCRIPTION_GRPC_AUTH_URL not set.");
                options.Secret = configuration["SUBSCRIPTION_GRPC_SECRET"]
                    ?? throw new InvalidOperationException("SUBSCRIPTION_GRPC_SECRET not set.");
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PaymentCallbackOptions>()
            .Configure(options =>
            {
                options.Secret = configuration["PAYMENT_CALLBACK_SECRET"]
                    ?? throw new InvalidOperationException("PAYMENT_CALLBACK_SECRET not set.");
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RabbitMqOptions>()
            .Configure(options =>
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
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        CurrencyOptions currencyOptions = new CurrencyOptions
        {
            Default = NormalizeCurrencyCode(configuration["PAYMENT_CURRENCY_DEFAULT"]) ?? "BOB",
            Allowed = ParseAllowedCurrencies(configuration["PAYMENT_CURRENCY_ALLOWED"])
        };
        currencyOptions.Validate();
        services.Configure<CurrencyOptions>(options =>
        {
            options.Default = currencyOptions.Default;
            options.Allowed = currencyOptions.Allowed;
        });

        string encryptionKeyBase64 = configuration["TODOTIX_APPKEY_ENCRYPTION_KEY"]
            ?? throw new InvalidOperationException("TODOTIX_APPKEY_ENCRYPTION_KEY not set.");
        services.AddSingleton<IAppKeyCipher>(_ => new AppKeyCipher(Convert.FromBase64String(encryptionKeyBase64)));
    }

    private static string? NormalizeCurrencyCode(string? rawCode)
    {
        string normalized = (rawCode ?? string.Empty).Trim().ToUpperInvariant();
        return normalized.Length == 0 ? null : normalized;
    }

    private static IReadOnlyList<string> ParseAllowedCurrencies(string? rawAllowed)
    {
        string[] parsed = (rawAllowed ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.ToUpperInvariant())
            .ToArray();
        return parsed.Length == 0 ? new[] { "BOB" } : parsed;
    }
}
