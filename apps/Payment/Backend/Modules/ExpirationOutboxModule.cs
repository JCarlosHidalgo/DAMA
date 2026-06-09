using Backend.Entities;
using Backend.Messaging;
using Backend.Services.Abstract;
using Backend.Workers;

namespace Backend.Modules;

public sealed class ExpirationOutboxModule : IServiceModule
{
    public int Order => 94;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOutboxPublisher<ExpirationOutboxEvent>, RabbitMqExpirationPublisher>();
        services.AddHostedService<OutboxRelayWorker<ExpirationOutboxEvent>>();
        services.AddHostedService<OutboxJanitor<ExpirationOutboxEvent>>();
    }
}
