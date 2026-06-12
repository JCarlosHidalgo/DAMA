using Backend.Grpc.Services;

namespace Backend.Modules;

public sealed class GrpcServerModule : IServiceModule, IAppModule
{
    int IServiceModule.Order => 100;
    int IAppModule.Order => 110;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddGrpc();
    }

    public void Configure(WebApplication app)
    {
        app.MapGrpcService<TenantSubscriptionGrpcService>().AllowAnonymous();
    }
}
