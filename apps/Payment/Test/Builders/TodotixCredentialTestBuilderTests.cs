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
    private Mock<ICallbackSignature> _callbackSignature = null!;
    private TodotixOptions _todotixOptions = null!;
    private TodotixCredentialTestBuilder _sut = null!;

    [SetUp]
    public void Setup()
    {
        _callbackSignature = new Mock<ICallbackSignature>(MockBehavior.Strict);
        _todotixOptions = new TodotixOptions
        {
            CallbackUrl = "https://payment.example.com/api/payment/qr/callback"
        };
        _sut = new TodotixCredentialTestBuilder(_callbackSignature.Object, Options.Create(_todotixOptions));
    }

    [Test]
    public void BuildCredentialTestRequest_BuildsGenericDebtOfValueOne()
    {
        _callbackSignature.Setup(s => s.Sign(It.IsAny<string>())).Returns("sig123");

        RegisterDebtRequest request = _sut.BuildCredentialTestRequest("custom-app-key", "America/La_Paz");

        Assert.Multiple(() =>
        {
            Assert.That(request.Appkey, Is.EqualTo("custom-app-key"));
            Assert.That(Guid.TryParse(request.IdentificadorDeuda, out _), Is.True);
            Assert.That(request.EmailCliente, Is.EqualTo("example@email.com"));
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
        _callbackSignature.Setup(s => s.Sign(It.IsAny<string>())).Returns("sig");

        RegisterDebtRequest first = _sut.BuildCredentialTestRequest("k", "America/La_Paz");
        RegisterDebtRequest second = _sut.BuildCredentialTestRequest("k", "America/La_Paz");

        Assert.That(first.IdentificadorDeuda, Is.Not.EqualTo(second.IdentificadorDeuda));
    }
}
