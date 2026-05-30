using Backend.DB.Daos.Abstract.Single.Attendance;
using Backend.Services.Abstract.Attendance;

namespace Backend.Modules;

public sealed class AutoRegisteredServicesModule : IServiceModule
{
    public int Order => 80;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<IScheduledClassService>()
            .AddClasses(classes => classes.InNamespaces(
                "Backend.Services.Concrete",
                "Backend.Claims",
                "Backend.Builders"))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
            .FromAssemblyOf<IScheduledClassAttendanceDao>()
            .AddClasses(classes => classes.InNamespaces("Backend.DB.Daos.Concrete.Single"))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }
}
