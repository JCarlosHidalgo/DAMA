using Backend.Grpc.Interceptors;
using Backend.Options;

using DAMA.Software.GrpcContracts;

using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

using Polly;

namespace Backend.Modules;

public sealed class GrpcClientsModule : IServiceModule
{
    public int Order => 91;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<SubscriptionSecretClientInterceptor>();

        services.AddGrpcClient<TenantSubscription.TenantSubscriptionClient>((serviceProvider, grpcClientOptions) =>
            {
                SubscriptionGrpcOptions options =
                    serviceProvider.GetRequiredService<IOptions<SubscriptionGrpcOptions>>().Value;
                grpcClientOptions.Address = new Uri(options.AuthUrl);
            })
            .AddInterceptor<SubscriptionSecretClientInterceptor>()
            .AddStandardResilienceHandler(ConfigureResilience);
    }

    private static void ConfigureResilience(HttpStandardResilienceOptions resilienceOptions)
    {
        resilienceOptions.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
        resilienceOptions.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
        resilienceOptions.Retry.MaxRetryAttempts = 2;
        resilienceOptions.Retry.Delay = TimeSpan.FromMilliseconds(200);
        resilienceOptions.Retry.BackoffType = DelayBackoffType.Exponential;
        resilienceOptions.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        resilienceOptions.CircuitBreaker.FailureRatio = 0.5;
        resilienceOptions.CircuitBreaker.MinimumThroughput = 5;
        resilienceOptions.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
    }
}
