using Backend.Workers;

namespace Backend.Modules;

public sealed class SubscriptionMaintenanceModule : IServiceModule
{
    public int Order => 95;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<SubscriptionExpiryJanitor>();
    }
}
