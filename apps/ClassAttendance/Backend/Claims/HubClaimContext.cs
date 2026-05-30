using System.Security.Claims;

using Backend.Security;

namespace Backend.Claims;

public sealed class HubClaimContext : IHubClaimContext
{
    public Guid GetTenantId(ClaimsPrincipal? user)
    {
        string? raw = user?.FindFirstValue(AuthClaims.TenantId);
        if (!Guid.TryParse(raw, out Guid tenantId))
        {
            throw new MissingClaimException(AuthClaims.TenantId);
        }

        return tenantId;
    }
}
