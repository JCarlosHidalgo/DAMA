namespace Backend.Modules;

public sealed class AuthorizationModule : IAppModule
{
    public int Order => 40;

    public void Configure(WebApplication app)
    {
        app.UseAuthorization();
    }
}
