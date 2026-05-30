using Backend.Dtos.Users.Input;
using Backend.Entities.Tenants;
using Backend.Entities.Users;

using Microsoft.AspNetCore.Identity;

namespace Backend.Builders;

public class UserEntityBuilder : IUserEntityBuilder
{
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserEntityBuilder(IPasswordHasher<User> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public User BuildUser(ICredentialsPayload request, UserRole role)
    {
        User user = new User();
        string hashedPassword = _passwordHasher.HashPassword(user, request.Password);
        user.Id = Guid.NewGuid();
        user.UserName = request.Username;
        user.PasswordHash = hashedPassword;
        user.Role = role.Value;
        return user;
    }

    public TenantDomain BuildTenantDomain(Guid userId, Guid tenantId)
    {
        return new TenantDomain
        {
            UserId = userId,
            TenantId = tenantId
        };
    }
}
