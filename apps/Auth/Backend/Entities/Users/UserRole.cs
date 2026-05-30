namespace Backend.Entities.Users;

public sealed class UserRole : IEquatable<UserRole>
{
    public static readonly UserRole Student = new(UserRoles.Student);
    public static readonly UserRole Teacher = new(UserRoles.Teacher);
    public static readonly UserRole Client = new(UserRoles.Client);

    public string Value { get; }

    private UserRole(string value)
    {
        Value = value;
    }

    public static UserRole From(string raw) => raw switch
    {
        UserRoles.Student => Student,
        UserRoles.Teacher => Teacher,
        UserRoles.Client => Client,
        _ => throw new ArgumentOutOfRangeException(nameof(raw), raw, "Unknown user role.")
    };

    public bool Equals(UserRole? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is UserRole other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static bool operator ==(UserRole? left, UserRole? right) =>
        left?.Equals(right) ?? right is null;
    public static bool operator !=(UserRole? left, UserRole? right) => !(left == right);
}
