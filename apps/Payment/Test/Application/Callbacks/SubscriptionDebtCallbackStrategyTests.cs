using Backend.Application.Callbacks;
using Backend.Builders;
using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Entities.Subscriptions;
using Backend.Options;
using Backend.Services.Abstract.Subscriptions;
using Backend.Services.Abstract.Todotix;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.Extensions.Options;

using Moq;

using Test.Infrastructure;

namespace Test.Application.Callbacks;

[TestFixture]
public class SubscriptionDebtCallbackStrategyTests
{
    private Mock<IPendingSubscriptionPaymentDao> _pendingDao = null!;
    private Mock<ISuccessSubscriptionPaymentDao> _successDao = null!;
    private Mock<IFailedSubscriptionPaymentDao> _failedDao = null!;
    private Mock<ISubscriptionPlanDao> _planDao = null!;
    private Mock<ITodotixClient> _todotixClient = null!;
    private Mock<IAuthSubscriptionUpdater> _authSubscriptionUpdater = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private Mock<ISubscriptionTransitionBuilder> _transitionBuilder = null!;
    private SubscriptionDebtCallbackStrategy _sut = null!;

    [SetUp]
    public void Setup()
    {
        _pendingDao = new Mock<IPendingSubscriptionPaymentDao>(MockBehavior.Strict);
        _successDao = new Mock<ISuccessSubscriptionPaymentDao>(MockBehavior.Strict);
        _failedDao = new Mock<IFailedSubscriptionPaymentDao>(MockBehavior.Strict);
        _planDao = new Mock<ISubscriptionPlanDao>(MockBehavior.Strict);
        _todotixClient = new Mock<ITodotixClient>(MockBehavior.Strict);
        _authSubscriptionUpdater = new Mock<IAuthSubscriptionUpdater>(MockBehavior.Strict);
        (_unitOfWork, _transactionScope) = UnitOfWorkMockHelper.BuildCommittingMocks();
        _transitionBuilder = new Mock<ISubscriptionTransitionBuilder>(MockBehavior.Strict);

        IOptions<TodotixOptions> options = Options.Create(new TodotixOptions { PlatformAppKey = "platform-key" });

        _sut = new SubscriptionDebtCallbackStrategy(
            _pendingDao.Object,
            _successDao.Object,
            _failedDao.Object,
            _planDao.Object,
            _todotixClient.Object,
            _authSubscriptionUpdater.Object,
            _unitOfWork.Object,
            _transitionBuilder.Object,
            options);
    }

    [Test]
    public async Task TryHandleAsync_PendingMissing_ReturnsFalseWithoutSideEffects()
    {
        Guid txId = Guid.NewGuid();
        _pendingDao.Setup(d => d.GetByIdAsync(txId)).ReturnsAsync((PendingSubscriptionPayment?)null);

        bool handled = await _sut.TryHandleAsync(txId);

        Assert.That(handled, Is.False);
        _authSubscriptionUpdater.Verify(
            u => u.UpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWork.Verify(unit => unit.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task TryHandleAsync_Paid_UpdatesAuthBeforeTransitioningToSuccess()
    {
        Guid txId = Guid.NewGuid();
        PendingSubscriptionPayment pending = new() { Id = txId, TenantId = Guid.NewGuid(), Level = 2, Cost = 180 };
        SubscriptionPlan plan = new() { Level = 2, Price = 180, DurationAmount = 1, DurationUnit = "Month" };
        SuccessSubscriptionPayment success = new() { Id = txId };

        _pendingDao.Setup(d => d.GetByIdAsync(txId)).ReturnsAsync(pending);
        _todotixClient.Setup(c => c.ConsultDebtAsync(txId, "platform-key")).ReturnsAsync(TodotixDebtState.Paid);
        _planDao.Setup(d => d.GetByLevelAsync(2)).ReturnsAsync(plan);
        _authSubscriptionUpdater
            .Setup(u => u.UpdateAsync(pending.TenantId, 2, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _transitionBuilder.Setup(b => b.BuildSuccessPayment(pending)).Returns(success);
        _successDao.Setup(d => d.TryCreateAsync(success, _transactionScope.Object)).ReturnsAsync(true);
        _pendingDao.Setup(d => d.DeleteAsync(pending.Id, _transactionScope.Object)).ReturnsAsync(true);

        bool handled = await _sut.TryHandleAsync(txId);

        Assert.That(handled, Is.True);
        _authSubscriptionUpdater.Verify(
            u => u.UpdateAsync(pending.TenantId, 2, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        _transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task TryHandleAsync_Unpaid_TransitionsToFailedWithoutUpdatingAuth()
    {
        Guid txId = Guid.NewGuid();
        PendingSubscriptionPayment pending = new() { Id = txId, TenantId = Guid.NewGuid(), Level = 1, Cost = 100 };
        FailedSubscriptionPayment failed = new() { Id = txId };

        _pendingDao.Setup(d => d.GetByIdAsync(txId)).ReturnsAsync(pending);
        _todotixClient.Setup(c => c.ConsultDebtAsync(txId, "platform-key")).ReturnsAsync(TodotixDebtState.Unpaid);
        _transitionBuilder.Setup(b => b.BuildFailedPayment(pending)).Returns(failed);
        _failedDao.Setup(d => d.TryCreateAsync(failed, _transactionScope.Object)).ReturnsAsync(true);
        _pendingDao.Setup(d => d.DeleteAsync(pending.Id, _transactionScope.Object)).ReturnsAsync(true);

        bool handled = await _sut.TryHandleAsync(txId);

        Assert.That(handled, Is.True);
        _authSubscriptionUpdater.Verify(
            u => u.UpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }
}
