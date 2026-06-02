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
    private Mock<IPendingSubscriptionPaymentDao> pendingDao = null!;
    private Mock<ISuccessSubscriptionPaymentDao> successDao = null!;
    private Mock<IFailedSubscriptionPaymentDao> failedDao = null!;
    private Mock<ISubscriptionPlanDao> planDao = null!;
    private Mock<ITodotixClient> todotixClient = null!;
    private Mock<IAuthSubscriptionUpdater> authSubscriptionUpdater = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private Mock<ISubscriptionTransitionBuilder> transitionBuilder = null!;
    private SubscriptionDebtCallbackStrategy sut = null!;

    [SetUp]
    public void Setup()
    {
        pendingDao = new Mock<IPendingSubscriptionPaymentDao>(MockBehavior.Strict);
        successDao = new Mock<ISuccessSubscriptionPaymentDao>(MockBehavior.Strict);
        failedDao = new Mock<IFailedSubscriptionPaymentDao>(MockBehavior.Strict);
        planDao = new Mock<ISubscriptionPlanDao>(MockBehavior.Strict);
        todotixClient = new Mock<ITodotixClient>(MockBehavior.Strict);
        authSubscriptionUpdater = new Mock<IAuthSubscriptionUpdater>(MockBehavior.Strict);
        (unitOfWork, transactionScope) = UnitOfWorkMockHelper.BuildCommittingMocks();
        transitionBuilder = new Mock<ISubscriptionTransitionBuilder>(MockBehavior.Strict);

        IOptions<TodotixOptions> options = Options.Create(new TodotixOptions { PlatformAppKey = "platform-key" });

        sut = new SubscriptionDebtCallbackStrategy(
            pendingDao.Object,
            successDao.Object,
            failedDao.Object,
            planDao.Object,
            todotixClient.Object,
            authSubscriptionUpdater.Object,
            unitOfWork.Object,
            transitionBuilder.Object,
            options);
    }

    [Test]
    public async Task TryHandleAsync_PendingMissing_ReturnsFalseWithoutSideEffects()
    {
        Guid txId = Guid.NewGuid();
        pendingDao.Setup(d => d.GetByIdAsync(txId)).ReturnsAsync((PendingSubscriptionPayment?)null);

        bool handled = await sut.TryHandleAsync(txId);

        Assert.That(handled, Is.False);
        authSubscriptionUpdater.Verify(
            u => u.UpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(unit => unit.BeginAsync(), Times.Never);
    }

    [Test]
    public async Task TryHandleAsync_Paid_UpdatesAuthBeforeTransitioningToSuccess()
    {
        Guid txId = Guid.NewGuid();
        PendingSubscriptionPayment pending = new() { Id = txId, TenantId = Guid.NewGuid(), Level = 2, Cost = 180 };
        SubscriptionPlan plan = new() { Level = 2, Price = 180, DurationAmount = 1, DurationUnit = "Month" };
        SuccessSubscriptionPayment success = new() { Id = txId };

        pendingDao.Setup(d => d.GetByIdAsync(txId)).ReturnsAsync(pending);
        todotixClient.Setup(c => c.ConsultDebtAsync(txId, "platform-key")).ReturnsAsync(TodotixDebtState.Paid);
        planDao.Setup(d => d.GetByLevelAsync(2)).ReturnsAsync(plan);
        authSubscriptionUpdater
            .Setup(u => u.UpdateAsync(pending.TenantId, 2, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        transitionBuilder.Setup(b => b.BuildSuccessPayment(pending)).Returns(success);
        successDao.Setup(d => d.TryCreateAsync(success, transactionScope.Object)).ReturnsAsync(true);
        pendingDao.Setup(d => d.DeleteAsync(pending.Id, transactionScope.Object)).ReturnsAsync(true);

        bool handled = await sut.TryHandleAsync(txId);

        Assert.That(handled, Is.True);
        authSubscriptionUpdater.Verify(
            u => u.UpdateAsync(pending.TenantId, 2, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task TryHandleAsync_Unpaid_TransitionsToFailedWithoutUpdatingAuth()
    {
        Guid txId = Guid.NewGuid();
        PendingSubscriptionPayment pending = new() { Id = txId, TenantId = Guid.NewGuid(), Level = 1, Cost = 100 };
        FailedSubscriptionPayment failed = new() { Id = txId };

        pendingDao.Setup(d => d.GetByIdAsync(txId)).ReturnsAsync(pending);
        todotixClient.Setup(c => c.ConsultDebtAsync(txId, "platform-key")).ReturnsAsync(TodotixDebtState.Unpaid);
        transitionBuilder.Setup(b => b.BuildFailedPayment(pending)).Returns(failed);
        failedDao.Setup(d => d.TryCreateAsync(failed, transactionScope.Object)).ReturnsAsync(true);
        pendingDao.Setup(d => d.DeleteAsync(pending.Id, transactionScope.Object)).ReturnsAsync(true);

        bool handled = await sut.TryHandleAsync(txId);

        Assert.That(handled, Is.True);
        authSubscriptionUpdater.Verify(
            u => u.UpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }
}
