using System.Security.Claims;

namespace Backend.Claims;

public interface IHubClaimContext
{
    Guid GetTenantId(ClaimsPrincipal? user);
}
