namespace Backend.Modules;

public sealed class AutoRegisteredServicesModule : IServiceModule
{
    public int Order => 80;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<Program>()
            .AddClasses(classes => classes.InNamespaces(
                "Backend.Services.Concrete",
                "Backend.Application",
                "Backend.DB.Daos.Concrete",
                "Backend.Claims",
                "Backend.Builders"))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }
}
