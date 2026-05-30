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
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private Mock<IProcessedEventDao> processedEventDao = null!;
    private Mock<IPendingQrPaymentDao> pendingDao = null!;
    private Mock<IFailedQrPaymentDao> failedDao = null!;
    private Mock<IQrPaymentTransitionBuilder> transitionBuilder = null!;
    private DebtExpiredHandler sut = null!;

    [SetUp]
    public void Setup()
    {
        (unitOfWork, transactionScope) = UnitOfWorkMockHelper.BuildCommittingMocks();
        processedEventDao = new Mock<IProcessedEventDao>(MockBehavior.Strict);
        pendingDao = new Mock<IPendingQrPaymentDao>(MockBehavior.Strict);
        failedDao = new Mock<IFailedQrPaymentDao>(MockBehavior.Strict);
        transitionBuilder = new Mock<IQrPaymentTransitionBuilder>(MockBehavior.Strict);

        sut = new DebtExpiredHandler(
            unitOfWork.Object,
            processedEventDao.Object,
            pendingDao.Object,
            failedDao.Object,
            transitionBuilder.Object,
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

        processedEventDao.Setup(d => d.TryMarkProcessedAsync(debtEvent.EventId, transactionScope.Object)).ReturnsAsync(true);
        pendingDao.Setup(d => d.GetByIdAsync(debtEvent.Data.PendingId)).ReturnsAsync(pending);
        transitionBuilder.Setup(b => b.BuildFailedPayment(pending)).Returns(failed);
        failedDao.Setup(d => d.TryCreateAsync(failed, transactionScope.Object)).ReturnsAsync(true);
        pendingDao.Setup(d => d.DeleteAsync(pending.Id, transactionScope.Object)).ReturnsAsync(true);

        HandleDebtExpiredOutcome outcome = await sut.HandleAsync(debtEvent, CancellationToken.None);

        Assert.That(outcome, Is.TypeOf<HandleDebtExpiredOutcome.Processed>());
        transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task HandleAsync_AlreadyProcessed_ReturnsAlreadyProcessedAndCommits()
    {
        DebtExpiredEvent debtEvent = NewEvent();
        processedEventDao.Setup(d => d.TryMarkProcessedAsync(debtEvent.EventId, transactionScope.Object)).ReturnsAsync(false);

        HandleDebtExpiredOutcome outcome = await sut.HandleAsync(debtEvent, CancellationToken.None);

        Assert.That(outcome, Is.TypeOf<HandleDebtExpiredOutcome.AlreadyProcessed>());
        transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task HandleAsync_PendingMissing_ReturnsPendingMissing()
    {
        DebtExpiredEvent debtEvent = NewEvent();
        processedEventDao.Setup(d => d.TryMarkProcessedAsync(debtEvent.EventId, transactionScope.Object)).ReturnsAsync(true);
        pendingDao.Setup(d => d.GetByIdAsync(debtEvent.Data.PendingId)).ReturnsAsync((PendingQrPayment?)null);

        HandleDebtExpiredOutcome outcome = await sut.HandleAsync(debtEvent, CancellationToken.None);

        Assert.That(outcome, Is.TypeOf<HandleDebtExpiredOutcome.PendingMissing>());
        transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenBeginThrows_ReturnsFailed()
    {
        DebtExpiredEvent debtEvent = NewEvent();
        Mock<IUnitOfWork> failing = new(MockBehavior.Strict);
        failing.Setup(u => u.BeginAsync()).ThrowsAsync(new InvalidOperationException("oops"));
        sut = new DebtExpiredHandler(failing.Object, processedEventDao.Object, pendingDao.Object, failedDao.Object, transitionBuilder.Object, NullLogger<DebtExpiredHandler>.Instance);

        HandleDebtExpiredOutcome outcome = await sut.HandleAsync(debtEvent, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.TypeOf<HandleDebtExpiredOutcome.Failed>());
            Assert.That(((HandleDebtExpiredOutcome.Failed)outcome).Reason, Does.Contain("oops"));
        });
    }
}
