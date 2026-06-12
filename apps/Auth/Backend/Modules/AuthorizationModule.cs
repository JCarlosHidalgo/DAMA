using Microsoft.AspNetCore.Authorization;

namespace Backend.Modules;

public sealed class AuthorizationModule : IServiceModule, IAppModule
{
    int IServiceModule.Order => 40;
    int IAppModule.Order => 40;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
    }

    public void Configure(WebApplication app)
    {
        app.UseAuthorization();
    }
}
