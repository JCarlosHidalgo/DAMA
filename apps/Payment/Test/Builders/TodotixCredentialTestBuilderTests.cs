using Backend.Builders;
using Backend.Dtos.External.Todotix;
using Backend.Options;
using Backend.Services.Abstract;

using Microsoft.Extensions.Options;

using Moq;

namespace Test.Builders;

[TestFixture]
public class TodotixCredentialTestBuilderTests
{
    private Mock<ICallbackSignature> callbackSignature = null!;
    private TodotixOptions todotixOptions = null!;
    private TodotixCredentialTestBuilder sut = null!;

    [SetUp]
    public void Setup()
    {
        callbackSignature = new Mock<ICallbackSignature>(MockBehavior.Strict);
        todotixOptions = new TodotixOptions
        {
            ApplicationKey = "appkey-xyz",
            CallbackUrl = "https://payment.example.com/api/payment/qr/callback"
        };
        sut = new TodotixCredentialTestBuilder(callbackSignature.Object, Options.Create(todotixOptions));
    }

    [Test]
    public void BuildCredentialTestRequest_BuildsGenericDebtOfValueOne()
    {
        callbackSignature.Setup(s => s.Sign(It.IsAny<string>())).Returns("sig123");

        RegisterDebtRequest request = sut.BuildCredentialTestRequest("custom-app-key", "America/La_Paz");

        Assert.Multiple(() =>
        {
            Assert.That(request.Appkey, Is.EqualTo("custom-app-key"));
            Assert.That(Guid.TryParse(request.IdentificadorDeuda, out _), Is.True);
            Assert.That(request.EmailCliente, Is.Null);
            Assert.That(request.LineasDetalleDeuda, Has.Count.EqualTo(1));
            Assert.That(request.CallbackUrl, Does.Contain("sig=sig123"));
            Assert.That(request.FechaVencimiento, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(request.LineasDetalleDeuda[0].Cantidad, Is.EqualTo(1));
            Assert.That(request.LineasDetalleDeuda[0].CostoUnitario, Is.EqualTo(1));
            Assert.That(request.LineasDetalleDeuda[0].DescuentoUnitario, Is.EqualTo(0));
        });
    }

    [Test]
    public void BuildCredentialTestRequest_MintsAFreshIdentifierEachCall()
    {
        callbackSignature.Setup(s => s.Sign(It.IsAny<string>())).Returns("sig");

        RegisterDebtRequest first = sut.BuildCredentialTestRequest("k", "America/La_Paz");
        RegisterDebtRequest second = sut.BuildCredentialTestRequest("k", "America/La_Paz");

        Assert.That(first.IdentificadorDeuda, Is.Not.EqualTo(second.IdentificadorDeuda));
    }
}
