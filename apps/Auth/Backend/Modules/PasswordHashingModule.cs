using Backend.Entities.Users;

using Microsoft.AspNetCore.Identity;

namespace Backend.Modules;

public sealed class PasswordHashingModule : IServiceModule
{
    public int Order => 45;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
    }
}
