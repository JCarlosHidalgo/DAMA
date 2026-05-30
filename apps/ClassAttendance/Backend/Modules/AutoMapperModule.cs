using Backend.AutoMapperProfiles;

namespace Backend.Modules;

public sealed class AutoMapperModule : IServiceModule
{
    public int Order => 75;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(mapperConfiguration => mapperConfiguration.AddProfile<AttendanceProfile>());
    }
}
