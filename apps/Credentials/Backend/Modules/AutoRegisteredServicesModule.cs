using Backend.Services.Abstract;

namespace Backend.Modules;

public sealed class AutoRegisteredServicesModule : IServiceModule
{
    public int Order => 80;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<ICredentialsService>()
            .AddClasses(classes => classes.InNamespaces(
                "Backend.Services.Concrete",
                "Backend.Claims"))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }
}
