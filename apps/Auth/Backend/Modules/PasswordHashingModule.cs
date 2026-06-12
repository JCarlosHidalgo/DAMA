using Backend.Entities.Users;

using Microsoft.AspNetCore.Identity;

namespace Backend.Modules;

public sealed class PasswordHashingModule : IServiceModule
{
    private const int Pbkdf2IterationCount = 210_000;

    public int Order => 45;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PasswordHasherOptions>(options =>
        {
            options.IterationCount = Pbkdf2IterationCount;
        });
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
    }
}
