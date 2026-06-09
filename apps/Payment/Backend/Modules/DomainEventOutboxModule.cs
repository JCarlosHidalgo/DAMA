using Backend.Entities;
using Backend.Messaging;
using Backend.Services.Abstract;
using Backend.Workers;

namespace Backend.Modules;

public sealed class DomainEventOutboxModule : IServiceModule
{
    public int Order => 93;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOutboxPublisher<OutboxEvent>, RabbitMqDomainEventPublisher>();
        services.AddHostedService<OutboxRelayWorker<OutboxEvent>>();
        services.AddHostedService<OutboxJanitor<OutboxEvent>>();
    }
}
