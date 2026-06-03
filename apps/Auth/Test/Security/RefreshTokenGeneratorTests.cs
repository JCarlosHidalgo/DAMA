using Backend.Options;
using Backend.Security;
using Backend.Transporters.Entities;

using Microsoft.Extensions.Options;

namespace Test.Security;

[TestFixture]
public class RefreshTokenGeneratorTests
{
    private RefreshTokenGenerator CreateSut(TimeSpan refreshLifetime)
    {
        JwtOptions options = new()
        {
            Issuer = "AuthIssuer",
            Audience = "Auth",
            Audiences = "Auth",
            PublicKey = "unused",
            PrivateKey = "unused",
            RefreshTokenLifetime = refreshLifetime
        };
        return new RefreshTokenGenerator(Options.Create(options));
    }

    [Test]
    public void Issue_ProducesSixtyFourCharLowerHexHashMatchingRawToken()
    {
        RefreshTokenGenerator sut = CreateSut(TimeSpan.FromDays(30));
        Guid userId = Guid.NewGuid();

        IssuedRefreshToken issued = sut.Issue(userId);

        Assert.Multiple(() =>
        {
            Assert.That(issued.Entity.UserId, Is.EqualTo(userId));
            Assert.That(issued.Entity.TokenHash, Has.Length.EqualTo(64));
            Assert.That(issued.Entity.TokenHash, Is.EqualTo(issued.Entity.TokenHash.ToLowerInvariant()));
            Assert.That(issued.Entity.TokenHash, Is.EqualTo(sut.ComputeHash(issued.RawToken)));
            Assert.That(issued.RawToken, Is.Not.Empty);
        });
    }

    [Test]
    public void Issue_StampsExpiryFromRefreshLifetime()
    {
        TimeSpan lifetime = TimeSpan.FromDays(30);
        RefreshTokenGenerator sut = CreateSut(lifetime);

        IssuedRefreshToken issued = sut.Issue(Guid.NewGuid());

        DateTime expectedExpiry = DateTime.UtcNow.Add(lifetime);
        Assert.That(issued.Entity.ExpiresAt, Is.EqualTo(expectedExpiry).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void Issue_ProducesDistinctRawTokensPerCall()
    {
        RefreshTokenGenerator sut = CreateSut(TimeSpan.FromDays(30));

        IssuedRefreshToken first = sut.Issue(Guid.NewGuid());
        IssuedRefreshToken second = sut.Issue(Guid.NewGuid());

        Assert.That(first.RawToken, Is.Not.EqualTo(second.RawToken));
    }

    [Test]
    public void ComputeHash_IsDeterministic()
    {
        RefreshTokenGenerator sut = CreateSut(TimeSpan.FromDays(30));

        string firstHash = sut.ComputeHash("same-token");
        string secondHash = sut.ComputeHash("same-token");

        Assert.That(secondHash, Is.EqualTo(firstHash));
    }
}
