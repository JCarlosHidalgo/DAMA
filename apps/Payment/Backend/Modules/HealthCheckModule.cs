using Backend.ExternalCheck;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Backend.Modules;

public sealed class HealthCheckModule : IServiceModule, IAppModule
{
    int IServiceModule.Order => 220;
    int IAppModule.Order => 5;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        string authUrl = configuration["SUBSCRIPTION_GRPC_AUTH_URL"]
                         ?? throw new InvalidOperationException("SUBSCRIPTION_GRPC_AUTH_URL is not configured.");

        services.AddHealthChecks()
            .AddTypeActivatedCheck<DatabaseHealthCheck>(
                ExternalCheckNaming.Name(ExternalDependency.Database),
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"])
            .AddTypeActivatedCheck<RabbitMqHealthCheck>(
                ExternalCheckNaming.Name(ExternalDependency.RabbitMq),
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"])
            .AddTypeActivatedCheck<GrpcPeerHealthCheck>(
                ExternalCheckNaming.Name(ExternalDependency.AuthGrpc),
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"],
                args: authUrl);
    }

    public void Configure(WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => false
        })
        .AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("ready"),
            ResponseWriter = ReadinessResponseWriter.WriteAsync
        })
        .AllowAnonymous();
    }
}
