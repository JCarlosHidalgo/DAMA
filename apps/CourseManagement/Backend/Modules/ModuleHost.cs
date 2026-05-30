namespace Backend.Modules;

public static class ModuleHost
{
    public static void RegisterAll(WebApplicationBuilder builder)
    {
        IEnumerable<IServiceModule> serviceModules = Discover().OfType<IServiceModule>();
        foreach (IServiceModule serviceModule in serviceModules
                     .OrderBy(module => module.Order)
                     .ThenBy(module => module.GetType().FullName, StringComparer.Ordinal))
        {
            serviceModule.Register(builder.Services, builder.Configuration);
        }
    }

    public static void ConfigureAll(WebApplication app)
    {
        IEnumerable<IAppModule> appModules = Discover().OfType<IAppModule>();
        foreach (IAppModule appModule in appModules
                     .OrderBy(module => module.Order)
                     .ThenBy(module => module.GetType().FullName, StringComparer.Ordinal))
        {
            appModule.Configure(app);
        }
    }

    private static IEnumerable<object> Discover()
    {
        return typeof(ModuleHost).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .Where(type => type.Namespace == "Backend.Modules")
            .Where(type => typeof(IServiceModule).IsAssignableFrom(type)
                        || typeof(IAppModule).IsAssignableFrom(type))
            .Select(type => Activator.CreateInstance(type)!);
    }
}
