using Backend.Messaging;
using Backend.Services.Abstract;
using Backend.Workers;

namespace Backend.Modules;

public sealed class OutboxProducerModule : IServiceModule
{
    public int Order => 93;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddHostedService<OutboxPublisher>();
        services.AddHostedService<OutboxJanitor>();
    }
}
