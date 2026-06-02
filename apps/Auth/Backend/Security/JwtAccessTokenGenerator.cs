using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Backend.Entities.Tenants;
using Backend.Entities.Users;
using Backend.Options;
using Backend.Services.Abstract;

using Microsoft.Extensions.Options;

namespace Backend.Security;

public sealed class JwtAccessTokenGenerator : IAccessTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly IJwtTokenSigner _tokenSigner;
    private readonly string[] _issuanceAudiences;

    public JwtAccessTokenGenerator(IOptions<JwtOptions> options, IJwtTokenSigner tokenSigner)
    {
        _options = options.Value;
        _tokenSigner = tokenSigner;
        _issuanceAudiences = _options.Audiences.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public string Issue(User user, Tenant tenant, TenantAllowedServices? allowedServices)
    {
        int indexCoreServicesPyramid = allowedServices?.IndexCoreServicesPyramid ?? 0;
        long subscriptionExpiresAt = allowedServices is null
            ? 0
            : new DateTimeOffset(DateTime.SpecifyKind(allowedServices.ExpiresAt, DateTimeKind.Utc)).ToUnixTimeSeconds();

        List<Claim> claims = new List<Claim>
        {
            new Claim(AuthClaims.TenantId, tenant.Id.ToString()),
            new Claim(AuthClaims.TenantName, tenant.Name),
            new Claim(AuthClaims.UserId, user.Id.ToString()),
            new Claim(AuthClaims.UserName, user.UserName),
            new Claim(AuthClaims.Role, user.Role),
            new Claim(AuthClaims.TenantTimezone, tenant.Timezone),
            new Claim(AuthClaims.IndexCoreServicesPyramid, indexCoreServicesPyramid.ToString()),
            new Claim(AuthClaims.SubscriptionExpiresAt, subscriptionExpiresAt.ToString())
        };
        foreach (string audience in _issuanceAudiences)
        {
            claims.Add(new Claim("aud", audience));
        }

        JwtSecurityToken securityToken = new JwtSecurityToken(
            issuer: _options.Issuer,
            claims: claims,
            expires: DateTime.UtcNow.Add(_options.Lifetime),
            signingCredentials: _tokenSigner.Credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(securityToken);
    }
}
