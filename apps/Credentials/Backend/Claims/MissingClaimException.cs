namespace Backend.Claims;

public sealed class MissingClaimException(string claimName) : Exception($"Required JWT claim '{claimName}' is missing or malformed.")
{
    public string ClaimName { get; } = claimName;
}
