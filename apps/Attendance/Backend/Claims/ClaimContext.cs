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
