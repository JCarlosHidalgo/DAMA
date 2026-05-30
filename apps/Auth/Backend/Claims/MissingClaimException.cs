namespace Backend.Claims;

public sealed class MissingClaimException : Exception
{
    public string ClaimName { get; }

    public MissingClaimException(string claimName)
        : base($"Required JWT claim '{claimName}' is missing or malformed.")
    {
        ClaimName = claimName;
    }
}
