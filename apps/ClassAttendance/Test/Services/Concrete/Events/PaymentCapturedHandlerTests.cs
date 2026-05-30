using Backend.DB.Daos.Abstract.Single.Events;
using Backend.DB.Daos.Abstract.Single.Remain;
using Backend.Events;
using Backend.Results.Events;
using Backend.Services.Concrete.Events;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Test.Services.Concrete.Events;

[TestFixture]
public class PaymentCapturedHandlerTests
{
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<IProcessedEventDao> processedEventDao = null!;
    private Mock<IStudentRemainClassesDao> remainClassesDao = null!;
    private Mock<IPaymentCreditLedgerDao> paymentCreditLedgerDao = null!;
    private Mock<ITransactionScope> transactionScope = null!;

    private PaymentCapturedHandler sut = null!;

    [SetUp]
    public void SetUp()
    {
        unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        processedEventDao = new Mock<IProcessedEventDao>(MockBehavior.Strict);
        remainClassesDao = new Mock<IStudentRemainClassesDao>(MockBehavior.Strict);
        paymentCreditLedgerDao = new Mock<IPaymentCreditLedgerDao>(MockBehavior.Strict);
        transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);

        sut = new PaymentCapturedHandler(
            unitOfWork.Object,
            processedEventDao.Object,
            remainClassesDao.Object,
            paymentCreditLedgerDao.Object,
            NullLogger<PaymentCapturedHandler>.Instance);
    }

    private static PaymentCapturedEvent BuildEvent() => new()
    {
        EventId = Guid.NewGuid(),
        EventType = "PaymentCaptured",
        OccurredAt = DateTime.UtcNow,
        Data = new PaymentCapturedData
        {
            TenantId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            Quantity = 4,
            ExternalReference = "pay-1"
        }
    };

    [Test]
    public async Task HandleAsync_FirstTime_IncrementsRemainAndRecordsLedgerAndReturnsRemainCredited()
    {
        PaymentCapturedEvent paymentEvent = BuildEvent();

        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(paymentEvent.EventId, transactionScope.Object))
            .ReturnsAsync(true);
        remainClassesDao
            .Setup(target => target.IncrementAsync(
                paymentEvent.Data.TenantId,
                paymentEvent.Data.StudentId,
                paymentEvent.Data.Quantity,
                null,
                transactionScope.Object))
            .Returns(Task.CompletedTask);
        paymentCreditLedgerDao
            .Setup(target => target.RecordAsync(
                paymentEvent.EventId,
                paymentEvent.Data.TenantId,
                paymentEvent.Data.StudentId,
                paymentEvent.Data.Quantity,
                paymentEvent.Data.ExternalReference,
                paymentEvent.OccurredAt,
                transactionScope.Object))
            .Returns(Task.CompletedTask);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        PaymentCapturedOutcome result = await sut.HandleAsync(paymentEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<PaymentCapturedOutcome.RemainCredited>());
    }

    [Test]
    public async Task HandleAsync_DuplicateEventId_ReturnsAlreadyProcessed()
    {
        PaymentCapturedEvent paymentEvent = BuildEvent();

        unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(transactionScope.Object);
        processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(paymentEvent.EventId, transactionScope.Object))
            .ReturnsAsync(false);
        transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        PaymentCapturedOutcome result = await sut.HandleAsync(paymentEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<PaymentCapturedOutcome.AlreadyProcessed>());
        remainClassesDao.Verify(
            target => target.IncrementAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ITransactionContext>()),
            Times.Never);
        paymentCreditLedgerDao.Verify(
            target => target.RecordAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<ITransactionContext>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenExceptionThrown_ReturnsFailed()
    {
        PaymentCapturedEvent paymentEvent = BuildEvent();
        unitOfWork.Setup(target => target.BeginAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        PaymentCapturedOutcome result = await sut.HandleAsync(paymentEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<PaymentCapturedOutcome.Failed>());
    }
}
