namespace Backend.Modules;

public sealed class ProblemDetailsModule : IServiceModule, IAppModule
{
    int IServiceModule.Order => 210;
    int IAppModule.Order => 20;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();
    }

    public void Configure(WebApplication app)
    {
        app.UseExceptionHandler();
    }
}
