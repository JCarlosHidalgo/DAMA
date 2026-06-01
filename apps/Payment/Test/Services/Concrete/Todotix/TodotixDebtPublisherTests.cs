using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Dtos.External.Todotix;
using Backend.Entities.Todotix;
using Backend.Results.Todotix;
using Backend.Services.Abstract.Todotix;
using Backend.Services.Concrete.Todotix;

using Moq;

namespace Test.Services.Concrete.Todotix;

[TestFixture]
public class TodotixDebtPublisherTests
{
    private Mock<ITodotixClient> todotixClient = null!;
    private Mock<IPendingQrPaymentDao> pendingDao = null!;
    private Mock<ITodotixAppKeyResolver> appKeyResolver = null!;
    private TodotixDebtPublisher sut = null!;

    [SetUp]
    public void Setup()
    {
        todotixClient = new Mock<ITodotixClient>(MockBehavior.Strict);
        pendingDao = new Mock<IPendingQrPaymentDao>(MockBehavior.Strict);
        appKeyResolver = new Mock<ITodotixAppKeyResolver>(MockBehavior.Strict);
        appKeyResolver.Setup(r => r.ResolveAsync(It.IsAny<Guid>())).ReturnsAsync("tenant-app-key");
        sut = new TodotixDebtPublisher(todotixClient.Object, pendingDao.Object, appKeyResolver.Object);
    }

    private const string ValidPayload = "{\"appkey\":\"k\",\"identificador_deuda\":\"x\",\"descripcion\":\"d\",\"callback_url\":\"u\",\"lineas_detalle_deuda\":[]}";

    private static TodotixOutboxEvent NewEvent(string payload) => new TodotixOutboxEvent { Id = Guid.NewGuid(), PendingId = Guid.NewGuid(), TenantId = Guid.NewGuid(), PayloadJson = payload };

    private static TodotixOutboxEvent NewRetryEvent(string payload) => new TodotixOutboxEvent { Id = Guid.NewGuid(), PendingId = Guid.NewGuid(), TenantId = Guid.NewGuid(), PayloadJson = payload, Attempts = 1 };

    [Test]
    public async Task PublishAsync_WhenTodotixSucceeds_UpdatesQrUrlAndReturnsSuccess()
    {
        TodotixOutboxEvent outboxEvent = NewEvent("{\"appkey\":\"k\",\"identificador_deuda\":\"x\",\"descripcion\":\"d\",\"callback_url\":\"u\",\"lineas_detalle_deuda\":[]}");
        var response = new RegisterDebtResponse { Error = 0, QrSimpleUrl = "http://qr" };
        todotixClient.Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>())).ReturnsAsync(response);
        pendingDao.Setup(d => d.UpdateQrImageUrlAsync(outboxEvent.PendingId, "http://qr")).Returns(Task.CompletedTask);

        PublishOutcome outcome = await sut.PublishAsync(outboxEvent);

        Assert.That(outcome, Is.TypeOf<PublishOutcome.Success>());
        pendingDao.Verify(d => d.UpdateQrImageUrlAsync(outboxEvent.PendingId, "http://qr"), Times.Once);
    }

    [Test]
    public async Task PublishAsync_WhenJsonInvalid_ReturnsPermanentFailure()
    {
        TodotixOutboxEvent outboxEvent = NewEvent("not-json{");

        PublishOutcome outcome = await sut.PublishAsync(outboxEvent);

        Assert.That(outcome, Is.TypeOf<PublishOutcome.PermanentFailure>());
    }

    [Test]
    public async Task PublishAsync_WhenPayloadDeserializesAsNull_ReturnsPermanentFailure()
    {
        TodotixOutboxEvent outboxEvent = NewEvent("null");

        PublishOutcome outcome = await sut.PublishAsync(outboxEvent);

        Assert.That(outcome, Is.TypeOf<PublishOutcome.PermanentFailure>());
    }

    [Test]
    public async Task PublishAsync_WhenClientThrows_ReturnsTransientFailure()
    {
        TodotixOutboxEvent outboxEvent = NewEvent("{\"appkey\":\"k\",\"identificador_deuda\":\"x\",\"descripcion\":\"d\",\"callback_url\":\"u\",\"lineas_detalle_deuda\":[]}");
        todotixClient.Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>())).ThrowsAsync(new HttpRequestException("network down"));

        PublishOutcome outcome = await sut.PublishAsync(outboxEvent);

        var transient = (PublishOutcome.TransientFailure)outcome;
        Assert.That(transient.Reason, Does.Contain("network down"));
    }

    [Test]
    public async Task PublishAsync_WhenTodotixReportsError_ReturnsTransientFailure()
    {
        TodotixOutboxEvent outboxEvent = NewEvent("{\"appkey\":\"k\",\"identificador_deuda\":\"x\",\"descripcion\":\"d\",\"callback_url\":\"u\",\"lineas_detalle_deuda\":[]}");
        todotixClient.Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()))
                     .ReturnsAsync(new RegisterDebtResponse { Error = 7, Mensaje = "err", Existente = 0 });

        PublishOutcome outcome = await sut.PublishAsync(outboxEvent);

        Assert.That(outcome, Is.TypeOf<PublishOutcome.TransientFailure>());
    }

    [Test]
    public void PublishAsync_WhenCancelled_RethrowsOperationCanceledException()
    {
        TodotixOutboxEvent outboxEvent = NewEvent(ValidPayload);
        todotixClient.Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>())).ThrowsAsync(new OperationCanceledException());

        Assert.ThrowsAsync<OperationCanceledException>(() => sut.PublishAsync(outboxEvent));
    }

    [Test]
    public async Task PublishAsync_OnRetryWhenDebtAlreadyExists_ReturnsPermanentFailureWithoutRegistering()
    {
        TodotixOutboxEvent outboxEvent = NewRetryEvent(ValidPayload);
        todotixClient.Setup(c => c.DebtExistsAsync(outboxEvent.PendingId, It.IsAny<string>())).ReturnsAsync(true);

        PublishOutcome outcome = await sut.PublishAsync(outboxEvent);

        Assert.That(outcome, Is.TypeOf<PublishOutcome.PermanentFailure>());
        todotixClient.Verify(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()), Times.Never);
        pendingDao.Verify(d => d.UpdateQrImageUrlAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task PublishAsync_OnRetryWhenDebtNotFound_ProceedsToRegister()
    {
        TodotixOutboxEvent outboxEvent = NewRetryEvent(ValidPayload);
        todotixClient.Setup(c => c.DebtExistsAsync(outboxEvent.PendingId, It.IsAny<string>())).ReturnsAsync(false);
        todotixClient.Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()))
                     .ReturnsAsync(new RegisterDebtResponse { Error = 0, QrSimpleUrl = "http://qr" });
        pendingDao.Setup(d => d.UpdateQrImageUrlAsync(outboxEvent.PendingId, "http://qr")).Returns(Task.CompletedTask);

        PublishOutcome outcome = await sut.PublishAsync(outboxEvent);

        Assert.That(outcome, Is.TypeOf<PublishOutcome.Success>());
        todotixClient.Verify(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()), Times.Once);
    }

    [Test]
    public async Task PublishAsync_OnRetryWhenExistenceCheckThrows_ReturnsTransientFailureWithoutRegistering()
    {
        TodotixOutboxEvent outboxEvent = NewRetryEvent(ValidPayload);
        todotixClient.Setup(c => c.DebtExistsAsync(outboxEvent.PendingId, It.IsAny<string>())).ThrowsAsync(new HttpRequestException("consult down"));

        PublishOutcome outcome = await sut.PublishAsync(outboxEvent);

        var transient = (PublishOutcome.TransientFailure)outcome;
        Assert.That(transient.Reason, Does.Contain("existence check failed"));
        todotixClient.Verify(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()), Times.Never);
    }

    [Test]
    public async Task PublishAsync_WhenRegisterReportsAlreadyExistsWithoutQr_ReturnsPermanentFailure()
    {
        TodotixOutboxEvent outboxEvent = NewEvent(ValidPayload);
        todotixClient.Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()))
                     .ReturnsAsync(new RegisterDebtResponse { Error = 0, QrSimpleUrl = null, Existente = 1 });

        PublishOutcome outcome = await sut.PublishAsync(outboxEvent);

        Assert.That(outcome, Is.TypeOf<PublishOutcome.PermanentFailure>());
        pendingDao.Verify(d => d.UpdateQrImageUrlAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task PublishAsync_WhenRegisterReturnsQrEvenIfExistente_PersistsQrAndReturnsSuccess()
    {
        TodotixOutboxEvent outboxEvent = NewEvent(ValidPayload);
        todotixClient.Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()))
                     .ReturnsAsync(new RegisterDebtResponse { Error = 0, QrSimpleUrl = "http://qr", Existente = 1 });
        pendingDao.Setup(d => d.UpdateQrImageUrlAsync(outboxEvent.PendingId, "http://qr")).Returns(Task.CompletedTask);

        PublishOutcome outcome = await sut.PublishAsync(outboxEvent);

        Assert.That(outcome, Is.TypeOf<PublishOutcome.Success>());
        pendingDao.Verify(d => d.UpdateQrImageUrlAsync(outboxEvent.PendingId, "http://qr"), Times.Once);
    }
}
