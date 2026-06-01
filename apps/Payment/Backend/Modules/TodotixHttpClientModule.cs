using Backend.Services.Abstract.Todotix;
using Backend.Services.Concrete.Todotix;

using Polly;

namespace Backend.Modules;

public sealed class TodotixHttpClientModule : IServiceModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<ITodotixClient, TodotixClient>(client =>
        {
            string baseUrl = configuration["Todotix:BaseUrl"]
                             ?? throw new InvalidOperationException("Todotix:BaseUrl is not configured.");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(25);
            options.Retry.ShouldHandle = static _ => PredicateResult.False();
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        });
    }
}
