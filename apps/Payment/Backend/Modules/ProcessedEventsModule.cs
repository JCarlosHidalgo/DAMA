using Backend.Workers;
using Backend.Workers.Events;

namespace Backend.Modules;

public sealed class ProcessedEventsModule : IServiceModule
{
    public int Order => 92;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<ProcessedEventsJanitor>();
        services.AddHostedService<DebtExpiredConsumer>();
    }
}
