using Backend.Builders;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities.QrPayments;
using Backend.Events;
using Backend.Results.QrPayments;
using Backend.Services.Concrete.Events;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Test.Infrastructure;

namespace Test.Services.Concrete.Events;

[TestFixture]
public class DebtExpiredHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private Mock<IProcessedEventDao> _processedEventDao = null!;
    private Mock<IPendingQrPaymentDao> _pendingDao = null!;
    private Mock<IFailedQrPaymentDao> _failedDao = null!;
    private Mock<IQrPaymentTransitionBuilder> _transitionBuilder = null!;
    private DebtExpiredHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        (_unitOfWork, _transactionScope) = UnitOfWorkMockHelper.BuildCommittingMocks();
        _processedEventDao = new Mock<IProcessedEventDao>(MockBehavior.Strict);
        _pendingDao = new Mock<IPendingQrPaymentDao>(MockBehavior.Strict);
        _failedDao = new Mock<IFailedQrPaymentDao>(MockBehavior.Strict);
        _transitionBuilder = new Mock<IQrPaymentTransitionBuilder>(MockBehavior.Strict);

        _sut = new DebtExpiredHandler(
            _unitOfWork.Object,
            _processedEventDao.Object,
            _pendingDao.Object,
            _failedDao.Object,
            _transitionBuilder.Object,
            NullLogger<DebtExpiredHandler>.Instance);
    }

    private static DebtExpiredEvent NewEvent()
    {
        return new DebtExpiredEvent
        {
            EventId = Guid.NewGuid(),
            EventType = "DebtExpired",
            OccurredAt = DateTime.UtcNow,
            AggregateId = Guid.NewGuid(),
            Data = new DebtExpiredData { PendingId = Guid.NewGuid(), TenantId = Guid.NewGuid(), StudentId = Guid.NewGuid() }
        };
    }

    [Test]
    public async Task HandleAsync_FirstTimeWithPending_TransitionsToFailedAndReturnsProcessed()
    {
        DebtExpiredEvent debtEvent = NewEvent();
        var pending = new PendingQrPayment { Id = debtEvent.Data.PendingId, TenantId = debtEvent.Data.TenantId };
        var failed = new FailedQrPayment { Id = pending.Id };

        _processedEventDao.Setup(d => d.TryMarkProcessedAsync(debtEvent.EventId, _transactionScope.Object)).ReturnsAsync(true);
        _pendingDao.Setup(d => d.GetByIdAsync(debtEvent.Data.PendingId)).ReturnsAsync(pending);
        _transitionBuilder.Setup(b => b.BuildFailedPayment(pending, FailureReason.Expired)).Returns(failed);
        _failedDao.Setup(d => d.TryCreateAsync(failed, _transactionScope.Object)).ReturnsAsync(true);
        _pendingDao.Setup(d => d.DeleteAsync(pending.Id, _transactionScope.Object)).ReturnsAsync(true);

        HandleDebtExpiredOutcome outcome = await _sut.HandleAsync(debtEvent, CancellationToken.None);

        Assert.That(outcome, Is.TypeOf<HandleDebtExpiredOutcome.Processed>());
        _transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task HandleAsync_AlreadyProcessed_ReturnsAlreadyProcessedAndCommits()
    {
        DebtExpiredEvent debtEvent = NewEvent();
        _processedEventDao.Setup(d => d.TryMarkProcessedAsync(debtEvent.EventId, _transactionScope.Object)).ReturnsAsync(false);

        HandleDebtExpiredOutcome outcome = await _sut.HandleAsync(debtEvent, CancellationToken.None);

        Assert.That(outcome, Is.TypeOf<HandleDebtExpiredOutcome.AlreadyProcessed>());
        _transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task HandleAsync_PendingMissing_ReturnsPendingMissing()
    {
        DebtExpiredEvent debtEvent = NewEvent();
        _processedEventDao.Setup(d => d.TryMarkProcessedAsync(debtEvent.EventId, _transactionScope.Object)).ReturnsAsync(true);
        _pendingDao.Setup(d => d.GetByIdAsync(debtEvent.Data.PendingId)).ReturnsAsync((PendingQrPayment?)null);

        HandleDebtExpiredOutcome outcome = await _sut.HandleAsync(debtEvent, CancellationToken.None);

        Assert.That(outcome, Is.TypeOf<HandleDebtExpiredOutcome.PendingMissing>());
        _transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenBeginThrows_ReturnsFailed()
    {
        DebtExpiredEvent debtEvent = NewEvent();
        Mock<IUnitOfWork> failing = new(MockBehavior.Strict);
        failing.Setup(u => u.BeginAsync()).ThrowsAsync(new InvalidOperationException("oops"));
        _sut = new DebtExpiredHandler(failing.Object, _processedEventDao.Object, _pendingDao.Object, _failedDao.Object, _transitionBuilder.Object, NullLogger<DebtExpiredHandler>.Instance);

        HandleDebtExpiredOutcome outcome = await _sut.HandleAsync(debtEvent, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.TypeOf<HandleDebtExpiredOutcome.Failed>());
            Assert.That(((HandleDebtExpiredOutcome.Failed)outcome).Reason, Does.Contain("oops"));
        });
    }
}
