using Backend.Messaging;
using Backend.Workers.Infrastructure;

namespace Backend.Modules;

public sealed class RabbitMqInfrastructureModule : IServiceModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddSingleton<RabbitMqTopologyDeclarer>();
        services.AddSingleton(typeof(RabbitMqMessageDispatcher<>));
        services.AddTransient<RabbitMqPublisherChannel>();
    }
}
