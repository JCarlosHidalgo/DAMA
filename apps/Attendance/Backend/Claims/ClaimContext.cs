using System.Security.Claims;

using Backend.Security;

namespace Backend.Claims;

public sealed class ClaimContext : IClaimContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _tenantId;
    private string? _tenantName;
    private string? _tenantTimezone;
    private Guid? _userId;
    private string? _userName;
    private string? _role;
    private int? _indexCoreServicesPyramid;
    private DateTime? _subscriptionExpiresAt;

    public ClaimContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId => _tenantId ??= ReadGuidClaim(AuthClaims.TenantId);

    public string TenantName => _tenantName ??= ReadStringClaim(AuthClaims.TenantName);

    public string TenantTimezone => _tenantTimezone ??= ReadStringClaim(AuthClaims.TenantTimezone);

    public Guid UserId => _userId ??= ReadGuidClaim(AuthClaims.UserId);

    public string UserName => _userName ??= ReadStringClaim(AuthClaims.UserName);

    public string Role => _role ??= ReadStringClaim(AuthClaims.Role);

    public int IndexCoreServicesPyramid =>
        _indexCoreServicesPyramid ??= ReadIntClaim(AuthClaims.IndexCoreServicesPyramid);

    public DateTime SubscriptionExpiresAt =>
        _subscriptionExpiresAt ??= ReadUnixSecondsClaim(AuthClaims.SubscriptionExpiresAt);

    private int ReadIntClaim(string claimName)
    {
        string raw = ReadStringClaim(claimName);
        if (!int.TryParse(raw, out int value))
        {
            throw new MissingClaimException(claimName);
        }
        return value;
    }

    private DateTime ReadUnixSecondsClaim(string claimName)
    {
        string raw = ReadStringClaim(claimName);
        if (!long.TryParse(raw, out long value))
        {
            throw new MissingClaimException(claimName);
        }
        return DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime;
    }

    private Guid ReadGuidClaim(string claimName)
    {
        string raw = ReadStringClaim(claimName);
        if (!Guid.TryParse(raw, out Guid value))
        {
            throw new MissingClaimException(claimName);
        }
        return value;
    }

    private string ReadStringClaim(string claimName)
    {
        string? raw = _httpContextAccessor.HttpContext?.User.FindFirstValue(claimName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new MissingClaimException(claimName);
        }
        return raw;
    }
}
