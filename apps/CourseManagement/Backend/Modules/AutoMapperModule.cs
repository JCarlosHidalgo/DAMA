using Backend.AutoMapperProfiles;

namespace Backend.Modules;

public sealed class AutoMapperModule : IServiceModule
{
    public int Order => 75;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<CourseProfile>();
            cfg.AddProfile<ScheduledClassProfile>();
            cfg.AddProfile<UniqueClassProfile>();
            cfg.AddProfile<ClassTeacherProfile>();
            cfg.AddProfile<ClassGroupProfile>();
        });
    }
}
