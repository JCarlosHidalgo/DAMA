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
    private Mock<IPendingQrPaymentDao> _pendingDao = null!;
    private Mock<ISuccessQrPaymentDao> _successDao = null!;
    private Mock<IFailedQrPaymentDao> _failedDao = null!;
    private Mock<IOutboxEventDao> _outboxEventDao = null!;
    private Mock<ITodotixClient> _todotixClient = null!;
    private Mock<ITodotixAppKeyResolver> _appKeyResolver = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private Mock<IQrPaymentTransitionBuilder> _transitionBuilder = null!;
    private ClassDebtCallbackStrategy _sut = null!;

    [SetUp]
    public void Setup()
    {
        _pendingDao = new Mock<IPendingQrPaymentDao>(MockBehavior.Strict);
        _successDao = new Mock<ISuccessQrPaymentDao>(MockBehavior.Strict);
        _failedDao = new Mock<IFailedQrPaymentDao>(MockBehavior.Strict);
        _outboxEventDao = new Mock<IOutboxEventDao>(MockBehavior.Strict);
        _todotixClient = new Mock<ITodotixClient>(MockBehavior.Strict);
        _appKeyResolver = new Mock<ITodotixAppKeyResolver>(MockBehavior.Strict);
        _appKeyResolver.Setup(r => r.ResolveAsync(It.IsAny<Guid>())).ReturnsAsync("tenant-app-key");
        (_unitOfWork, _transactionScope) = UnitOfWorkMockHelper.BuildCommittingMocks();
        _transitionBuilder = new Mock<IQrPaymentTransitionBuilder>(MockBehavior.Strict);

        _sut = new ClassDebtCallbackStrategy(
            _pendingDao.Object,
            _successDao.Object,
            _failedDao.Object,
            _outboxEventDao.Object,
            _todotixClient.Object,
            _appKeyResolver.Object,
            _unitOfWork.Object,
            _transitionBuilder.Object);
    }

    [Test]
    public async Task HandleCallbackAsync_PendingMissing_ExitsWithoutSideEffects()
    {
        var txId = Guid.NewGuid();
        _pendingDao.Setup(d => d.GetByIdAsync(txId)).ReturnsAsync((PendingQrPayment?)null);

        bool handled = await _sut.TryHandleAsync(txId);

        Assert.That(handled, Is.False);
        _unitOfWork.Verify(unit => unit.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task HandleCallbackAsync_Paid_TransitionsToSuccessAndPublishesOutbox()
    {
        var txId = Guid.NewGuid();
        var pending = new PendingQrPayment { Id = txId, TenantId = Guid.NewGuid(), StudentId = Guid.NewGuid(), ClassQuantity = 5, Cost = 50 };
        _pendingDao.Setup(d => d.GetByIdAsync(txId)).ReturnsAsync(pending);
        _todotixClient.Setup(c => c.ConsultDebtAsync(txId, It.IsAny<string>())).ReturnsAsync(TodotixDebtState.Paid);

        var successPayment = new SuccessQrPayment { Id = txId };
        var capturedEvent = new OutboxEvent { Id = txId };
        _transitionBuilder.Setup(b => b.BuildSuccessPayment(pending)).Returns(successPayment);
        _transitionBuilder.Setup(b => b.BuildCapturedOutboxEvent(pending)).Returns(capturedEvent);
        _successDao.Setup(d => d.TryCreateAsync(successPayment, _transactionScope.Object)).ReturnsAsync(true);
        _pendingDao.Setup(d => d.DeleteAsync(pending.Id, _transactionScope.Object)).ReturnsAsync(true);
        _outboxEventDao.Setup(d => d.InsertAsync(capturedEvent, _transactionScope.Object)).Returns(Task.CompletedTask);

        bool handled = await _sut.TryHandleAsync(txId);

        Assert.That(handled, Is.True);
        _transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task HandleCallbackAsync_Unpaid_TransitionsToFailed()
    {
        var txId = Guid.NewGuid();
        var pending = new PendingQrPayment { Id = txId, TenantId = Guid.NewGuid(), StudentId = Guid.NewGuid(), ClassQuantity = 5, Cost = 50 };
        _pendingDao.Setup(d => d.GetByIdAsync(txId)).ReturnsAsync(pending);
        _todotixClient.Setup(c => c.ConsultDebtAsync(txId, It.IsAny<string>())).ReturnsAsync(TodotixDebtState.Unpaid);

        var failedPayment = new FailedQrPayment { Id = txId };
        _transitionBuilder.Setup(b => b.BuildFailedPayment(pending)).Returns(failedPayment);
        _failedDao.Setup(d => d.TryCreateAsync(failedPayment, _transactionScope.Object)).ReturnsAsync(true);
        _pendingDao.Setup(d => d.DeleteAsync(pending.Id, _transactionScope.Object)).ReturnsAsync(true);

        bool handled = await _sut.TryHandleAsync(txId);

        Assert.That(handled, Is.True);
        _transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }
}
