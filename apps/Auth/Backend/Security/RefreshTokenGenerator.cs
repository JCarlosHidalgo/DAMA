using System.Security.Cryptography;
using System.Text;

using Backend.Entities.Tokens;
using Backend.Options;
using Backend.Transporters.Entities;

using Microsoft.Extensions.Options;

namespace Backend.Security;

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenByteLength = 32;

    private readonly JwtOptions _options;

    public RefreshTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public IssuedRefreshToken Issue(Guid userId)
    {
        string rawToken = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenByteLength));
        DateTime now = DateTime.UtcNow;
        RefreshToken entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = ComputeHash(rawToken),
            ExpiresAt = now.Add(_options.RefreshTokenLifetime),
            CreatedAt = now
        };
        return new IssuedRefreshToken(rawToken, entity);
    }

    public string ComputeHash(string rawToken)
    {
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
