using Backend.Workers.Events;

namespace Backend.Modules;

public sealed class EventConsumersModule : IServiceModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<StudentRegisteredConsumer>();
        services.AddHostedService<CourseDeletedConsumer>();
        services.AddHostedService<ClassDeletedConsumer>();
        services.AddHostedService<PaymentCapturedConsumer>();
    }
}
