namespace Backend.Modules;

public sealed class MvcModule : IServiceModule, IAppModule
{
    int IServiceModule.Order => 200;
    int IAppModule.Order => 100;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
    }

    public void Configure(WebApplication app)
    {
        app.MapControllers();
    }
}
