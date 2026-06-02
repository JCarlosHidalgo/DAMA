using Backend.Application.Subscriptions;
using Backend.Builders;
using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Entities;
using Backend.Entities.Subscriptions;
using Backend.Options;
using Backend.Services.Abstract.Subscriptions;
using Backend.Services.Abstract.Todotix;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.Extensions.Options;

namespace Backend.Application.Callbacks;

public sealed class SubscriptionDebtCallbackStrategy : DebtCallbackStrategyBase<PendingSubscriptionPayment>
{
    private readonly IPendingSubscriptionPaymentDao _pendingSubscriptionPaymentDao;
    private readonly ISuccessSubscriptionPaymentDao _successSubscriptionPaymentDao;
    private readonly IFailedSubscriptionPaymentDao _failedSubscriptionPaymentDao;
    private readonly ISubscriptionPlanDao _subscriptionPlanDao;
    private readonly IAuthSubscriptionUpdater _authSubscriptionUpdater;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISubscriptionTransitionBuilder _transitionBuilder;
    private readonly IOptions<TodotixOptions> _todotixOptions;

    public SubscriptionDebtCallbackStrategy(IPendingSubscriptionPaymentDao pendingSubscriptionPaymentDao,
                                            ISuccessSubscriptionPaymentDao successSubscriptionPaymentDao,
                                            IFailedSubscriptionPaymentDao failedSubscriptionPaymentDao,
                                            ISubscriptionPlanDao subscriptionPlanDao,
                                            ITodotixClient todotixClient,
                                            IAuthSubscriptionUpdater authSubscriptionUpdater,
                                            IUnitOfWork unitOfWork,
                                            ISubscriptionTransitionBuilder transitionBuilder,
                                            IOptions<TodotixOptions> todotixOptions)
        : base(todotixClient)
    {
        _pendingSubscriptionPaymentDao = pendingSubscriptionPaymentDao;
        _successSubscriptionPaymentDao = successSubscriptionPaymentDao;
        _failedSubscriptionPaymentDao = failedSubscriptionPaymentDao;
        _subscriptionPlanDao = subscriptionPlanDao;
        _authSubscriptionUpdater = authSubscriptionUpdater;
        _unitOfWork = unitOfWork;
        _transitionBuilder = transitionBuilder;
        _todotixOptions = todotixOptions;
    }

    public override DebtKind Kind => DebtKind.TenantSubscription;

    protected override async Task<PendingSubscriptionPayment?> LoadPendingAsync(Guid transactionId)
    {
        return await _pendingSubscriptionPaymentDao.GetByIdAsync(transactionId);
    }

    protected override Task<string?> ResolveAppKeyAsync(PendingSubscriptionPayment pending)
    {
        string platformAppKey = _todotixOptions.Value.PlatformAppKey;
        return Task.FromResult(string.IsNullOrWhiteSpace(platformAppKey) ? null : platformAppKey);
    }

    protected override async Task TransitionToSuccessAsync(PendingSubscriptionPayment pending)
    {
        DateTime newExpiresAtUtc = await ComputeNewExpiryAsync(pending.Level);

        await _authSubscriptionUpdater.UpdateAsync(pending.TenantId, pending.Level, newExpiresAtUtc);

        await _unitOfWork.RunInTransactionAsync(async transaction =>
        {
            SuccessSubscriptionPayment success = _transitionBuilder.BuildSuccessPayment(pending);
            await _successSubscriptionPaymentDao.TryCreateAsync(success, transaction);
            await _pendingSubscriptionPaymentDao.DeleteAsync(pending.Id, transaction);
        });
    }

    protected override async Task TransitionToFailedAsync(PendingSubscriptionPayment pending)
    {
        await _unitOfWork.RunInTransactionAsync(async transaction =>
        {
            FailedSubscriptionPayment failed = _transitionBuilder.BuildFailedPayment(pending);
            await _failedSubscriptionPaymentDao.TryCreateAsync(failed, transaction);
            await _pendingSubscriptionPaymentDao.DeleteAsync(pending.Id, transaction);
        });
    }

    private async Task<DateTime> ComputeNewExpiryAsync(int level)
    {
        DateTime capturedAtUtc = DateTime.UtcNow;
        SubscriptionPlan? plan = await _subscriptionPlanDao.GetByLevelAsync(level);
        if (plan is null)
        {
            return capturedAtUtc.AddMonths(1);
        }

        return SubscriptionExpiryCalculator.ComputeExpiryUtc(capturedAtUtc, plan.DurationAmount, plan.DurationUnit);
    }
}
