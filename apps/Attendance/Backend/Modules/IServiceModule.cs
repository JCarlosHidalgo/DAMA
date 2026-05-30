namespace Backend.Modules;

public interface IServiceModule
{
    int Order => 100;
    void Register(IServiceCollection services, IConfiguration configuration);
}
