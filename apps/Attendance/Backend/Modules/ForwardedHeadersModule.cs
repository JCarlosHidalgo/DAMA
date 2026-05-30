using Microsoft.AspNetCore.HttpOverrides;

namespace Backend.Modules;

public sealed class ForwardedHeadersModule : IServiceModule, IAppModule
{
    int IServiceModule.Order => 20;
    int IAppModule.Order => 10;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(forwardedHeadersOptions =>
        {
            forwardedHeadersOptions.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                                     | ForwardedHeaders.XForwardedProto
                                                     | ForwardedHeaders.XForwardedHost;
            forwardedHeadersOptions.KnownNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();
        });
    }

    public void Configure(WebApplication app)
    {
        app.UseForwardedHeaders();
    }
}
