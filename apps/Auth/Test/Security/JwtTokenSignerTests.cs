using Backend.Options;
using Backend.Security;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Test.Infrastructure;

namespace Test.Security;

[TestFixture]
public class JwtTokenSignerTests
{
    private TestRsaKeyPair rsaKeyPair = null!;
    private JwtOptions options = null!;

    [SetUp]
    public void SetUp()
    {
        rsaKeyPair = TestRsaKeyPair.Generate();
        options = new JwtOptions
        {
            Issuer = "Auth",
            Audience = "Auth",
            Audiences = "Auth",
            PublicKey = rsaKeyPair.PublicKeyBase64,
            PrivateKey = rsaKeyPair.PrivateKeyBase64
        };
    }

    [Test]
    public void Credentials_AfterConstruction_UsesRsaSha256Algorithm()
    {
        using JwtTokenSigner sut = new(Options.Create(options));

        Assert.That(sut.Credentials.Algorithm, Is.EqualTo(SecurityAlgorithms.RsaSha256));
    }

    [Test]
    public void Credentials_AfterConstruction_ExposesRsaSecurityKey()
    {
        using JwtTokenSigner sut = new(Options.Create(options));

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
            PublicKey = rsaKeyPair.PublicKeyBase64,
            PrivateKey = "not-base64!!!"
        };

        Assert.That(
            () => new JwtTokenSigner(Options.Create(invalidOptions)),
            Throws.InstanceOf<FormatException>());
    }

    [Test]
    public void Dispose_DoesNotThrow()
    {
        JwtTokenSigner sut = new(Options.Create(options));

        Assert.That(() => sut.Dispose(), Throws.Nothing);
    }
}
