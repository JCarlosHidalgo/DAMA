using Backend.Options;
using Backend.Services.Concrete;

using Microsoft.Extensions.Options;

namespace Test.Security;

[TestFixture]
public class CallbackSignatureTests
{
    private CallbackSignature sut = null!;

    [SetUp]
    public void Setup() => sut = new CallbackSignature(Options.Create(new PaymentCallbackOptions { Secret = "super-secret-key" }));

    [Test]
    public void Sign_ProducesBase64UrlWithoutPadding()
    {
        string signature = sut.Sign("payload-1");

        Assert.That(signature, Is.Not.Empty);
        Assert.That(signature, Does.Not.Contain("="));
        Assert.That(signature, Does.Not.Contain("+"));
        Assert.That(signature, Does.Not.Contain("/"));
    }

    [Test]
    public void Verify_RoundTripSucceeds()
    {
        string payload = "tx-id-abc";

        string signature = sut.Sign(payload);

        Assert.That(sut.Verify(payload, signature), Is.True);
    }

    [Test]
    public void Verify_WithDifferentPayload_Fails()
    {
        string signature = sut.Sign("p1");

        Assert.That(sut.Verify("p2", signature), Is.False);
    }

    [Test]
    public void Verify_WithEmptyPayloadOrSignature_Fails()
    {
        Assert.Multiple(() =>
        {
            Assert.That(sut.Verify(string.Empty, "sig"), Is.False);
            Assert.That(sut.Verify("payload", string.Empty), Is.False);
        });
    }

    [Test]
    public void Verify_WithMalformedBase64Url_Fails() => Assert.That(sut.Verify("payload", "%%%not-base64%%%"), Is.False);

    [Test]
    public void Verify_WithBase64UrlPaddingVariants_HandlesAllRemainders()
    {
        string signature = sut.Sign("payload-X");
        string twoCharPadded = signature.Length >= 2 ? signature[..^2] : signature;
        string threeCharPadded = signature.Length >= 1 ? signature[..^1] : signature;

        bool oneCharResult = sut.Verify("payload-X", signature + "A");
        bool twoCharResult = sut.Verify("payload-X", twoCharPadded);
        bool threeCharResult = sut.Verify("payload-X", threeCharPadded);

        Assert.Multiple(() =>
        {
            Assert.That(oneCharResult, Is.False);
            Assert.That(twoCharResult, Is.False);
            Assert.That(threeCharResult, Is.False);
        });
    }

    [Test]
    public void Sign_WithNullPayload_Throws() => Assert.Throws<ArgumentNullException>(() => sut.Sign(null!));
}
