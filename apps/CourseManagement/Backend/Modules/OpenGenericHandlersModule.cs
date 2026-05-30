using Backend.Application.Infrastructure;

namespace Backend.Modules;

public sealed class OpenGenericHandlersModule : IServiceModule
{
    public int Order => 85;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(typeof(IClassCreationCoordinator<>), typeof(ClassCreationCoordinator<>));
    }
}
