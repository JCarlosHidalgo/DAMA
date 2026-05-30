namespace Backend.Modules;

public interface IAppModule
{
    int Order => 100;
    void Configure(WebApplication app);
}
