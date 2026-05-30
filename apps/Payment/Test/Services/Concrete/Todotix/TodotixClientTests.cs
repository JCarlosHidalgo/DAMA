using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Backend.Dtos.External.Todotix;
using Backend.Options;
using Backend.Services.Abstract.Todotix;
using Backend.Services.Concrete.Todotix;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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

    private StubHandler handler = null!;
    private HttpClient httpClient = null!;
    private TodotixClient sut = null!;

    [SetUp]
    public void Setup()
    {
        handler = new StubHandler();
        httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://todotix.test") };
        sut = new TodotixClient(httpClient, Options.Create(new TodotixOptions { ApplicationKey = "k", CallbackUrl = "http://cb" }), NullLogger<TodotixClient>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        httpClient.Dispose();
        handler.Dispose();
    }

    [Test]
    public async Task RegisterDebtAsync_OnSuccess_ReturnsDeserializedBody()
    {
        var response = new RegisterDebtResponse { Error = 0, QrSimpleUrl = "http://q" };
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        RegisterDebtResponse body = await sut.RegisterDebtAsync(new RegisterDebtRequest { Appkey = "k" });

        Assert.Multiple(() =>
        {
            Assert.That(body.Error, Is.EqualTo(0));
            Assert.That(body.QrSimpleUrl, Is.EqualTo("http://q"));
            Assert.That(handler.LastRequest!.RequestUri!.AbsolutePath, Is.EqualTo("/rest/deuda/registrar"));
        });
    }

    [Test]
    public void RegisterDebtAsync_NonSuccessStatusCode_Throws()
    {
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));

        Assert.ThrowsAsync<HttpRequestException>(() => sut.RegisterDebtAsync(new RegisterDebtRequest()));
    }

    [Test]
    public void RegisterDebtAsync_EmptyBody_Throws()
    {
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

        Assert.ThrowsAsync<InvalidOperationException>(() => sut.RegisterDebtAsync(new RegisterDebtRequest()));
    }

    [Test]
    public async Task DebtExistsAsync_WhenErrorZeroAndDatosPresent_ReturnsTrue()
    {
        var response = new ConsultDebtResponse { Error = 0, Existente = 1, Datos = new ConsultDebtData { Identificador = "x" } };
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        bool exists = await sut.DebtExistsAsync(Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(exists, Is.True);
            Assert.That(handler.LastRequest!.RequestUri!.AbsolutePath, Is.EqualTo("/rest/deuda/consultar_deudas/por_identificador"));
        });
    }

    [Test]
    public async Task DebtExistsAsync_WhenErrorNonZero_ReturnsFalse()
    {
        var response = new ConsultDebtResponse { Error = 5, Mensaje = "no encontrada", Datos = null };
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        bool exists = await sut.DebtExistsAsync(Guid.NewGuid());

        Assert.That(exists, Is.False);
    }

    [Test]
    public async Task DebtExistsAsync_WhenDatosNull_ReturnsFalse()
    {
        var response = new ConsultDebtResponse { Error = 0, Existente = 0, Datos = null };
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        bool exists = await sut.DebtExistsAsync(Guid.NewGuid());

        Assert.That(exists, Is.False);
    }

    [Test]
    public void DebtExistsAsync_EmptyBody_Throws()
    {
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

        Assert.ThrowsAsync<InvalidOperationException>(() => sut.DebtExistsAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task ConsultDebtAsync_WhenPaid_ReturnsPaid()
    {
        var response = new ConsultDebtResponse { Error = 0, Existente = 1, Datos = new ConsultDebtData { Pagado = true, PagoAnulado = false } };
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        TodotixDebtState state = await sut.ConsultDebtAsync(Guid.NewGuid());

        Assert.That(state, Is.EqualTo(TodotixDebtState.Paid));
    }

    [Test]
    public async Task ConsultDebtAsync_WhenUnpaid_ReturnsUnpaid()
    {
        var response = new ConsultDebtResponse { Error = 0, Existente = 1, Datos = new ConsultDebtData { Pagado = false, PagoAnulado = false } };
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        TodotixDebtState state = await sut.ConsultDebtAsync(Guid.NewGuid());

        Assert.That(state, Is.EqualTo(TodotixDebtState.Unpaid));
    }

    [Test]
    public async Task ConsultDebtAsync_WhenPagoAnulado_ReturnsUnpaid()
    {
        var response = new ConsultDebtResponse { Error = 0, Existente = 1, Datos = new ConsultDebtData { Pagado = true, PagoAnulado = true } };
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        TodotixDebtState state = await sut.ConsultDebtAsync(Guid.NewGuid());

        Assert.That(state, Is.EqualTo(TodotixDebtState.Unpaid));
    }

    [Test]
    public async Task ConsultDebtAsync_WhenPaidAndExistenteZero_ReturnsPaid()
    {
        var response = new ConsultDebtResponse { Error = 0, Existente = 0, Datos = new ConsultDebtData { Pagado = true, PagoAnulado = false } };
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        TodotixDebtState state = await sut.ConsultDebtAsync(Guid.NewGuid());

        Assert.That(state, Is.EqualTo(TodotixDebtState.Paid));
    }

    [Test]
    public async Task ConsultDebtAsync_WhenErrorNonZero_ReturnsUnpaid()
    {
        var response = new ConsultDebtResponse { Error = 7, Existente = 1, Datos = new ConsultDebtData { Pagado = true, PagoAnulado = false } };
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
        });

        TodotixDebtState state = await sut.ConsultDebtAsync(Guid.NewGuid());

        Assert.That(state, Is.EqualTo(TodotixDebtState.Unpaid));
    }

    [Test]
    public void ConsultDebtAsync_EmptyBody_Throws()
    {
        handler.Responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

        Assert.ThrowsAsync<InvalidOperationException>(() => sut.ConsultDebtAsync(Guid.NewGuid()));
    }
}
