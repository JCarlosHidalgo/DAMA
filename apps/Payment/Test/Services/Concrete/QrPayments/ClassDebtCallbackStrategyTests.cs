using Backend.Application.Callbacks;
using Backend.Builders;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities;
using Backend.Entities.QrPayments;
using Backend.Services.Abstract.Todotix;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

using Test.Infrastructure;

namespace Test.Services.Concrete.QrPayments;

[TestFixture]
public class ClassDebtCallbackStrategyTests
{
    private Mock<IPendingQrPaymentDao> pendingDao = null!;
    private Mock<ISuccessQrPaymentDao> successDao = null!;
    private Mock<IFailedQrPaymentDao> failedDao = null!;
    private Mock<IOutboxEventDao> outboxEventDao = null!;
    private Mock<ITodotixClient> todotixClient = null!;
    private Mock<ITodotixAppKeyResolver> appKeyResolver = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private Mock<IQrPaymentTransitionBuilder> transitionBuilder = null!;
    private ClassDebtCallbackStrategy sut = null!;

    [SetUp]
    public void Setup()
    {
        pendingDao = new Mock<IPendingQrPaymentDao>(MockBehavior.Strict);
        successDao = new Mock<ISuccessQrPaymentDao>(MockBehavior.Strict);
        failedDao = new Mock<IFailedQrPaymentDao>(MockBehavior.Strict);
        outboxEventDao = new Mock<IOutboxEventDao>(MockBehavior.Strict);
        todotixClient = new Mock<ITodotixClient>(MockBehavior.Strict);
        appKeyResolver = new Mock<ITodotixAppKeyResolver>(MockBehavior.Strict);
        appKeyResolver.Setup(r => r.ResolveAsync(It.IsAny<Guid>())).ReturnsAsync("tenant-app-key");
        (unitOfWork, transactionScope) = UnitOfWorkMockHelper.BuildCommittingMocks();
        transitionBuilder = new Mock<IQrPaymentTransitionBuilder>(MockBehavior.Strict);

        sut = new ClassDebtCallbackStrategy(
            pendingDao.Object,
            successDao.Object,
            failedDao.Object,
            outboxEventDao.Object,
            todotixClient.Object,
            appKeyResolver.Object,
            unitOfWork.Object,
            transitionBuilder.Object);
    }

    [Test]
    public async Task HandleCallbackAsync_PendingMissing_ExitsWithoutSideEffects()
    {
        var txId = Guid.NewGuid();
        pendingDao.Setup(d => d.GetByIdAsync(txId)).ReturnsAsync((PendingQrPayment?)null);

        bool handled = await sut.TryHandleAsync(txId);

        Assert.That(handled, Is.False);
        unitOfWork.Verify(unit => unit.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task HandleCallbackAsync_Paid_TransitionsToSuccessAndPublishesOutbox()
    {
        var txId = Guid.NewGuid();
        var pending = new PendingQrPayment { Id = txId, TenantId = Guid.NewGuid(), StudentId = Guid.NewGuid(), ClassQuantity = 5, Cost = 50 };
        pendingDao.Setup(d => d.GetByIdAsync(txId)).ReturnsAsync(pending);
        todotixClient.Setup(c => c.ConsultDebtAsync(txId, It.IsAny<string>())).ReturnsAsync(TodotixDebtState.Paid);

        var successPayment = new SuccessQrPayment { Id = txId };
        var capturedEvent = new OutboxEvent { Id = txId };
        transitionBuilder.Setup(b => b.BuildSuccessPayment(pending)).Returns(successPayment);
        transitionBuilder.Setup(b => b.BuildCapturedOutboxEvent(pending)).Returns(capturedEvent);
        successDao.Setup(d => d.TryCreateAsync(successPayment, transactionScope.Object)).ReturnsAsync(true);
        pendingDao.Setup(d => d.DeleteAsync(pending.Id, transactionScope.Object)).ReturnsAsync(true);
        outboxEventDao.Setup(d => d.InsertAsync(capturedEvent, transactionScope.Object)).Returns(Task.CompletedTask);

        bool handled = await sut.TryHandleAsync(txId);

        Assert.That(handled, Is.True);
        transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task HandleCallbackAsync_Unpaid_TransitionsToFailed()
    {
        var txId = Guid.NewGuid();
        var pending = new PendingQrPayment { Id = txId, TenantId = Guid.NewGuid(), StudentId = Guid.NewGuid(), ClassQuantity = 5, Cost = 50 };
        pendingDao.Setup(d => d.GetByIdAsync(txId)).ReturnsAsync(pending);
        todotixClient.Setup(c => c.ConsultDebtAsync(txId, It.IsAny<string>())).ReturnsAsync(TodotixDebtState.Unpaid);

        var failedPayment = new FailedQrPayment { Id = txId };
        transitionBuilder.Setup(b => b.BuildFailedPayment(pending)).Returns(failedPayment);
        failedDao.Setup(d => d.TryCreateAsync(failedPayment, transactionScope.Object)).ReturnsAsync(true);
        pendingDao.Setup(d => d.DeleteAsync(pending.Id, transactionScope.Object)).ReturnsAsync(true);

        bool handled = await sut.TryHandleAsync(txId);

        Assert.That(handled, Is.True);
        transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }
}
