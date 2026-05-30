using Backend.Hubs;

namespace Backend.Modules;

public sealed class SignalRModule : IServiceModule, IAppModule
{
    int IServiceModule.Order => 100;
    int IAppModule.Order => 110;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR();
    }

    public void Configure(WebApplication app)
    {
        app.MapHub<AttendanceHub>("/hubs/attendance");
    }
}
