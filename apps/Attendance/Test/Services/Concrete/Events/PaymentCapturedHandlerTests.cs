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
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IProcessedEventDao> _processedEventDao = null!;
    private Mock<IStudentRemainClassesDao> _remainClassesDao = null!;
    private Mock<IPaymentCreditLedgerDao> _paymentCreditLedgerDao = null!;
    private Mock<ITransactionScope> _transactionScope = null!;

    private PaymentCapturedHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        _processedEventDao = new Mock<IProcessedEventDao>(MockBehavior.Strict);
        _remainClassesDao = new Mock<IStudentRemainClassesDao>(MockBehavior.Strict);
        _paymentCreditLedgerDao = new Mock<IPaymentCreditLedgerDao>(MockBehavior.Strict);
        _transactionScope = new Mock<ITransactionScope>(MockBehavior.Strict);

        _transactionScope.Setup(scope => scope.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _sut = new PaymentCapturedHandler(
            _unitOfWork.Object,
            _processedEventDao.Object,
            _remainClassesDao.Object,
            _paymentCreditLedgerDao.Object,
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

        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(paymentEvent.EventId, _transactionScope.Object))
            .ReturnsAsync(true);
        _remainClassesDao
            .Setup(target => target.IncrementAsync(
                paymentEvent.Data.TenantId,
                paymentEvent.Data.StudentId,
                paymentEvent.Data.Quantity,
                null,
                _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _paymentCreditLedgerDao
            .Setup(target => target.RecordAsync(
                paymentEvent.EventId,
                paymentEvent.Data.TenantId,
                paymentEvent.Data.StudentId,
                paymentEvent.Data.Quantity,
                paymentEvent.Data.ExternalReference,
                paymentEvent.OccurredAt,
                _transactionScope.Object))
            .Returns(Task.CompletedTask);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        PaymentCapturedOutcome result = await _sut.HandleAsync(paymentEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<PaymentCapturedOutcome.RemainCredited>());
    }

    [Test]
    public async Task HandleAsync_DuplicateEventId_ReturnsAlreadyProcessed()
    {
        PaymentCapturedEvent paymentEvent = BuildEvent();

        _unitOfWork.Setup(target => target.BeginAsync()).ReturnsAsync(_transactionScope.Object);
        _processedEventDao
            .Setup(target => target.TryMarkProcessedAsync(paymentEvent.EventId, _transactionScope.Object))
            .ReturnsAsync(false);
        _transactionScope.Setup(scope => scope.CommitAsync()).Returns(Task.CompletedTask);

        PaymentCapturedOutcome result = await _sut.HandleAsync(paymentEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<PaymentCapturedOutcome.AlreadyProcessed>());
        _remainClassesDao.Verify(
            target => target.IncrementAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<ITransactionContext>()),
            Times.Never);
        _paymentCreditLedgerDao.Verify(
            target => target.RecordAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<ITransactionContext>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_WhenExceptionThrown_ReturnsFailed()
    {
        PaymentCapturedEvent paymentEvent = BuildEvent();
        _unitOfWork.Setup(target => target.BeginAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        PaymentCapturedOutcome result = await _sut.HandleAsync(paymentEvent, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<PaymentCapturedOutcome.Failed>());
    }
}
