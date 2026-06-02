using System.IdentityModel.Tokens.Jwt;

using Backend.Entities.Tenants;
using Backend.Entities.Users;
using Backend.Options;
using Backend.Security;

using Microsoft.Extensions.Options;

using Test.Infrastructure;

namespace Test.Security;

[TestFixture]
public class JwtAccessTokenGeneratorTests
{
    private TestRsaKeyPair rsaKeyPair = null!;
    private JwtTokenSigner tokenSigner = null!;

    [SetUp]
    public void SetUp() => rsaKeyPair = TestRsaKeyPair.Generate();

    [TearDown]
    public void TearDown() => tokenSigner?.Dispose();

    private JwtAccessTokenGenerator CreateSut(string audiences, TimeSpan lifetime)
    {
        JwtOptions options = new()
        {
            Issuer = "AuthIssuer",
            Audience = "Auth",
            Audiences = audiences,
            PublicKey = rsaKeyPair.PublicKeyBase64,
            PrivateKey = rsaKeyPair.PrivateKeyBase64,
            Lifetime = lifetime
        };
        tokenSigner = new JwtTokenSigner(Options.Create(options));
        return new JwtAccessTokenGenerator(Options.Create(options), tokenSigner);
    }

    private static JwtSecurityToken ParseToken(string accessToken) =>
        new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

    [Test]
    public void Issue_EmitsUserAndTenantClaims()
    {
        JwtAccessTokenGenerator sut = CreateSut("Auth", TimeSpan.FromHours(1));
        User user = new()
        {
            Id = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            UserName = "the_user",
            Role = UserRole.Student.Value
        };
        Tenant tenant = new()
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Academia",
            Timezone = "America/La_Paz"
        };

        JwtSecurityToken parsedToken = ParseToken(sut.Issue(user, tenant, null));

        Assert.Multiple(() =>
        {
            Assert.That(ClaimValue(parsedToken, AuthClaims.TenantId), Is.EqualTo(tenant.Id.ToString()));
            Assert.That(ClaimValue(parsedToken, AuthClaims.TenantName), Is.EqualTo(tenant.Name));
            Assert.That(ClaimValue(parsedToken, AuthClaims.UserId), Is.EqualTo(user.Id.ToString()));
            Assert.That(ClaimValue(parsedToken, AuthClaims.UserName), Is.EqualTo(user.UserName));
            Assert.That(ClaimValue(parsedToken, AuthClaims.Role), Is.EqualTo(user.Role));
            Assert.That(ClaimValue(parsedToken, AuthClaims.TenantTimezone), Is.EqualTo(tenant.Timezone));
        });
    }

    [Test]
    public void Issue_WhenAllowedServicesPresent_EmitsIndexAndExpiryClaims()
    {
        JwtAccessTokenGenerator sut = CreateSut("Auth", TimeSpan.FromHours(1));
        User user = new() { Id = Guid.NewGuid(), UserName = "anyone", Role = UserRole.Client.Value };
        Tenant tenant = new() { Id = Guid.NewGuid(), Name = "Academia", Timezone = "America/La_Paz" };
        DateTime expiresAt = new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc);
        TenantAllowedServices allowedServices = new()
        {
            Id = tenant.Id,
            IndexCoreServicesPyramid = 2,
            ExpiresAt = expiresAt
        };

        JwtSecurityToken parsedToken = ParseToken(sut.Issue(user, tenant, allowedServices));

        Assert.Multiple(() =>
        {
            Assert.That(ClaimValue(parsedToken, AuthClaims.IndexCoreServicesPyramid), Is.EqualTo("2"));
            Assert.That(
                ClaimValue(parsedToken, AuthClaims.SubscriptionExpiresAt),
                Is.EqualTo(new DateTimeOffset(expiresAt).ToUnixTimeSeconds().ToString()));
        });
    }

    [Test]
    public void Issue_WhenAllowedServicesNull_EmitsZeroIndexAndEpochExpiry()
    {
        JwtAccessTokenGenerator sut = CreateSut("Auth", TimeSpan.FromHours(1));
        User user = new() { Id = Guid.NewGuid(), UserName = "anyone", Role = UserRole.Client.Value };
        Tenant tenant = new() { Id = Guid.NewGuid(), Name = "Academia", Timezone = "America/La_Paz" };

        JwtSecurityToken parsedToken = ParseToken(sut.Issue(user, tenant, null));

        Assert.Multiple(() =>
        {
            Assert.That(ClaimValue(parsedToken, AuthClaims.IndexCoreServicesPyramid), Is.EqualTo("0"));
            Assert.That(ClaimValue(parsedToken, AuthClaims.SubscriptionExpiresAt), Is.EqualTo("0"));
        });
    }

    [Test]
    public void Issue_EmitsOneAudienceClaimPerCommaSeparatedAudience()
    {
        JwtAccessTokenGenerator sut = CreateSut("Auth, Attendance ,Payment", TimeSpan.FromHours(1));
        User user = new() { Id = Guid.NewGuid(), UserName = "anyone", Role = UserRole.Student.Value };
        Tenant tenant = new() { Id = Guid.NewGuid(), Name = "Academia", Timezone = "America/La_Paz" };

        JwtSecurityToken parsedToken = ParseToken(sut.Issue(user, tenant, null));

        List<string> audiences = [.. parsedToken.Audiences];
        Assert.That(audiences, Has.Count.EqualTo(3));
        Assert.That(audiences, Does.Contain("Auth"));
        Assert.That(audiences, Does.Contain("Attendance"));
        Assert.That(audiences, Does.Contain("Payment"));
    }

    [Test]
    public void Issue_StampsIssuerFromOptions()
    {
        JwtAccessTokenGenerator sut = CreateSut("Auth", TimeSpan.FromHours(1));
        User user = new() { Id = Guid.NewGuid(), UserName = "anyone", Role = UserRole.Student.Value };
        Tenant tenant = new() { Id = Guid.NewGuid(), Name = "Academia", Timezone = "America/La_Paz" };

        JwtSecurityToken parsedToken = ParseToken(sut.Issue(user, tenant, null));

        Assert.That(parsedToken.Issuer, Is.EqualTo("AuthIssuer"));
    }

    [Test]
    public void Issue_ExpiryIsIssuanceTimePlusLifetime()
    {
        TimeSpan lifetime = TimeSpan.FromHours(24);
        JwtAccessTokenGenerator sut = CreateSut("Auth", lifetime);
        User user = new() { Id = Guid.NewGuid(), UserName = "anyone", Role = UserRole.Student.Value };
        Tenant tenant = new() { Id = Guid.NewGuid(), Name = "Academia", Timezone = "America/La_Paz" };

        DateTime expectedExpiry = DateTime.UtcNow.Add(lifetime);
        JwtSecurityToken parsedToken = ParseToken(sut.Issue(user, tenant, null));

        Assert.That(parsedToken.ValidTo, Is.EqualTo(expectedExpiry).Within(TimeSpan.FromSeconds(5)));
    }

    private static string? ClaimValue(JwtSecurityToken token, string claimType) =>
        token.Claims.FirstOrDefault(claim => claim.Type == claimType)?.Value;
}
