using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.Users;

public class User : IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Text(80)]
    public string UserName { get; set; } = string.Empty;

    [Text(200)]
    public string PasswordHash { get; set; } = string.Empty;

    [Text(50)]
    public string Role { get; set; } = string.Empty;

    [Flag]
    public bool IsDeleted { get; set; }

    [Integer]
    public int FailedLoginAttempts { get; set; }

    [PreciseTimestamp]
    public DateTime? LockedUntil { get; set; }
}
