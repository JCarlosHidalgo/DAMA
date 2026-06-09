using Backend.Workers.Infrastructure;

namespace Backend.Modules;

public sealed class RabbitMqInfrastructureModule : IServiceModule
{
    public int Order => 90;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddSingleton<RabbitMqTopologyDeclarer>();
        services.AddSingleton(typeof(RabbitMqMessageDispatcher<>));
    }
}
