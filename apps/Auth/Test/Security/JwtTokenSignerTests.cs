using Backend.Options;
using Backend.Security;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Test.Infrastructure;

namespace Test.Security;

[TestFixture]
public class JwtTokenSignerTests
{
    private TestRsaKeyPair _rsaKeyPair = null!;
    private JwtOptions _options = null!;

    [SetUp]
    public void SetUp()
    {
        _rsaKeyPair = TestRsaKeyPair.Generate();
        _options = new JwtOptions
        {
            Issuer = "Auth",
            Audience = "Auth",
            Audiences = "Auth",
            PublicKey = _rsaKeyPair.PublicKeyBase64,
            PrivateKey = _rsaKeyPair.PrivateKeyBase64
        };
    }

    [Test]
    public void Credentials_AfterConstruction_UsesRsaSha256Algorithm()
    {
        using JwtTokenSigner sut = new(Options.Create(_options));

        Assert.That(sut.Credentials.Algorithm, Is.EqualTo(SecurityAlgorithms.RsaSha256));
    }

    [Test]
    public void Credentials_AfterConstruction_ExposesRsaSecurityKey()
    {
        using JwtTokenSigner sut = new(Options.Create(_options));

        Assert.That(sut.Credentials.Key, Is.InstanceOf<RsaSecurityKey>());
    }

    [Test]
    public void Constructor_WhenPrivateKeyNotBase64_ThrowsFormatException()
    {
        JwtOptions invalidOptions = new()
        {
            Issuer = "Auth",
            Audience = "Auth",
            Audiences = "Auth",
            PublicKey = _rsaKeyPair.PublicKeyBase64,
            PrivateKey = "not-base64!!!"
        };

        Assert.That(
            () => new JwtTokenSigner(Options.Create(invalidOptions)),
            Throws.InstanceOf<FormatException>());
    }

    [Test]
    public void Dispose_DoesNotThrow()
    {
        JwtTokenSigner sut = new(Options.Create(_options));

        Assert.That(() => sut.Dispose(), Throws.Nothing);
    }
}
