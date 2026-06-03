using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Backend.Dtos.External.Todotix;
using Backend.Services.Abstract.Todotix;
using Backend.Services.Concrete.Todotix;

using Microsoft.Extensions.Logging.Abstractions;

namespace Test.Services.Concrete.Todotix;

[TestFixture]
public class TodotixClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public Func<HttpRequestMessage, Task<HttpResponseMessage>> Responder = _ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { }) });

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Responder(request);
        }
    }

    private StubHandler _handler = null!;
    private HttpClient _httpClient = null!;
    private TodotixClient _sut = null!;

    [SetUp]
    public void Setup()
    {
        _handler = new StubHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://todotix.test") };
        _sut = new TodotixClient(_httpClient, NullLogger<TodotixClient>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }

    [Test]
    public async Task RegisterDebtAsync_OnSuccess_ReturnsDeserializedBody()
    {
        var response = new RegisterDebtResponse { Error = 0, QrSimpleUrl = "http://q" };
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        RegisterDebtResponse body = await _sut.RegisterDebtAsync(new RegisterDebtRequest { Appkey = "k" });

        Assert.Multiple(() =>
        {
            Assert.That(body.Error, Is.EqualTo(0));
            Assert.That(body.QrSimpleUrl, Is.EqualTo("http://q"));
            Assert.That(_handler.LastRequest!.RequestUri!.AbsolutePath, Is.EqualTo("/rest/deuda/registrar"));
        });
    }

    [Test]
    public void RegisterDebtAsync_NonSuccessStatusCode_Throws()
    {
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));

        Assert.ThrowsAsync<HttpRequestException>(() => _sut.RegisterDebtAsync(new RegisterDebtRequest()));
    }

    [Test]
    public void RegisterDebtAsync_EmptyBody_Throws()
    {
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.RegisterDebtAsync(new RegisterDebtRequest()));
    }

    [Test]
    public async Task DebtExistsAsync_WhenErrorZeroAndDatosPresent_ReturnsTrue()
    {
        var response = new ConsultDebtResponse { Error = 0, Existente = 1, Datos = new ConsultDebtData { Identificador = "x" } };
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        bool exists = await _sut.DebtExistsAsync(Guid.NewGuid(), "k");

        Assert.Multiple(() =>
        {
            Assert.That(exists, Is.True);
            Assert.That(_handler.LastRequest!.RequestUri!.AbsolutePath, Is.EqualTo("/rest/deuda/consultar_deudas/por_identificador"));
        });
    }

    [Test]
    public async Task DebtExistsAsync_WhenErrorNonZero_ReturnsFalse()
    {
        var response = new ConsultDebtResponse { Error = 5, Mensaje = "no encontrada", Datos = null };
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        bool exists = await _sut.DebtExistsAsync(Guid.NewGuid(), "k");

        Assert.That(exists, Is.False);
    }

    [Test]
    public async Task DebtExistsAsync_WhenDatosNull_ReturnsFalse()
    {
        var response = new ConsultDebtResponse { Error = 0, Existente = 0, Datos = null };
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        bool exists = await _sut.DebtExistsAsync(Guid.NewGuid(), "k");

        Assert.That(exists, Is.False);
    }

    [Test]
    public void DebtExistsAsync_EmptyBody_Throws()
    {
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.DebtExistsAsync(Guid.NewGuid(), "k"));
    }

    [Test]
    public async Task ConsultDebtAsync_WhenPaid_ReturnsPaid()
    {
        var response = new ConsultDebtResponse { Error = 0, Existente = 1, Datos = new ConsultDebtData { Pagado = true, PagoAnulado = false } };
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        TodotixDebtState state = await _sut.ConsultDebtAsync(Guid.NewGuid(), "k");

        Assert.That(state, Is.EqualTo(TodotixDebtState.Paid));
    }

    [Test]
    public async Task ConsultDebtAsync_WhenUnpaid_ReturnsUnpaid()
    {
        var response = new ConsultDebtResponse { Error = 0, Existente = 1, Datos = new ConsultDebtData { Pagado = false, PagoAnulado = false } };
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        TodotixDebtState state = await _sut.ConsultDebtAsync(Guid.NewGuid(), "k");

        Assert.That(state, Is.EqualTo(TodotixDebtState.Unpaid));
    }

    [Test]
    public async Task ConsultDebtAsync_WhenPagoAnulado_ReturnsUnpaid()
    {
        var response = new ConsultDebtResponse { Error = 0, Existente = 1, Datos = new ConsultDebtData { Pagado = true, PagoAnulado = true } };
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        TodotixDebtState state = await _sut.ConsultDebtAsync(Guid.NewGuid(), "k");

        Assert.That(state, Is.EqualTo(TodotixDebtState.Unpaid));
    }

    [Test]
    public async Task ConsultDebtAsync_WhenPaidAndExistenteZero_ReturnsPaid()
    {
        var response = new ConsultDebtResponse { Error = 0, Existente = 0, Datos = new ConsultDebtData { Pagado = true, PagoAnulado = false } };
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        TodotixDebtState state = await _sut.ConsultDebtAsync(Guid.NewGuid(), "k");

        Assert.That(state, Is.EqualTo(TodotixDebtState.Paid));
    }

    [Test]
    public async Task ConsultDebtAsync_WhenErrorNonZero_ReturnsUnpaid()
    {
        var response = new ConsultDebtResponse { Error = 7, Existente = 1, Datos = new ConsultDebtData { Pagado = true, PagoAnulado = false } };
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        TodotixDebtState state = await _sut.ConsultDebtAsync(Guid.NewGuid(), "k");

        Assert.That(state, Is.EqualTo(TodotixDebtState.Unpaid));
    }

    [Test]
    public void ConsultDebtAsync_EmptyBody_Throws()
    {
        _handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ConsultDebtAsync(Guid.NewGuid(), "k"));
    }
}
