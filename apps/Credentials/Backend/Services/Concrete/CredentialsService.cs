using Backend.Claims;
using Backend.Dtos.Output;
using Backend.Services.Abstract;

namespace Backend.Services.Concrete;

public class CredentialsService : ICredentialsService
{
    private readonly IClaimContext _claimContext;

    public CredentialsService(IClaimContext claimContext)
    {
        _claimContext = claimContext;
    }

    public Task<UserClaimsDto> GetCredentials()
    {
        var userClaims = new UserClaimsDto
        {
            TenantId = _claimContext.TenantId.ToString(),
            TenantName = _claimContext.TenantName,
            UserId = _claimContext.UserId.ToString(),
            UserName = _claimContext.UserName,
            UserRole = _claimContext.Role
        };

        return Task.FromResult(userClaims);
    }
}
