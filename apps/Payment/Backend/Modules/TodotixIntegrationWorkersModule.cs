using Backend.Workers.QrPayments;
using Backend.Workers.Todotix;

namespace Backend.Modules;

public sealed class TodotixIntegrationWorkersModule : IServiceModule
{
    public int Order => 95;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<TodotixOutboxWorker>();
        services.AddHostedService<PaymentCallbackWorker>();
    }
}
