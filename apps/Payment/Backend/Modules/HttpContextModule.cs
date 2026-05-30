namespace Backend.Modules;

public sealed class HttpContextModule : IServiceModule
{
    public int Order => 30;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
    }
}
