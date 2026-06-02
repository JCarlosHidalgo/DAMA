using System.ComponentModel.DataAnnotations;

namespace Backend.Options;

public sealed class JwtOptions
{
    public const string SectionName = "AppSettings";

    [Required]
    public string Issuer { get; init; } = default!;

    [Required]
    public string Audience { get; init; } = default!;

    [Required]
    public string Audiences { get; init; } = default!;

    [Required]
    public string PublicKey { get; init; } = default!;

    [Required]
    public string PrivateKey { get; init; } = default!;

    public TimeSpan Lifetime { get; init; } = TimeSpan.FromDays(1);

    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(30);
}
