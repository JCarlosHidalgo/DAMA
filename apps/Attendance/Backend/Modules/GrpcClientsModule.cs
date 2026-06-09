using Backend.Grpc.Interceptors;

using DAMA.Software.GrpcContracts;

using Microsoft.Extensions.Http.Resilience;

using Polly;

namespace Backend.Modules;

public sealed class GrpcClientsModule : IServiceModule
{
    public int Order => 91;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<JwtForwardClientInterceptor>();

        services.AddGrpcClient<ClassExistence.ClassExistenceClient>(grpcClientOptions =>
            {
                string baseUrl = configuration["Services:CourseManagementUrl"]
                                 ?? throw new InvalidOperationException("Services:CourseManagementUrl is not configured.");
                grpcClientOptions.Address = new Uri(baseUrl);
            })
            .AddInterceptor<JwtForwardClientInterceptor>()
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
