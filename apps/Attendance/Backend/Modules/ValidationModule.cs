using FluentValidation;

namespace Backend.Modules;

public sealed class ValidationModule : IServiceModule
{
    public int Order => 70;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssemblyContaining<Program>();
    }
}
