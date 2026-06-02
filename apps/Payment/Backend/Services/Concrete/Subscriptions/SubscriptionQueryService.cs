using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Dtos.Subscriptions.Output;
using Backend.Entities.Subscriptions;
using Backend.Entities.Todotix;
using Backend.Results.QrPayments;
using Backend.Services.Abstract.Subscriptions;

namespace Backend.Services.Concrete.Subscriptions;

public class SubscriptionQueryService : ISubscriptionQueryService
{
    private readonly IPendingSubscriptionPaymentDao _pendingSubscriptionPaymentDao;
    private readonly ISuccessSubscriptionPaymentDao _successSubscriptionPaymentDao;
    private readonly ITodotixOutboxDao _todotixOutboxDao;
    private readonly ISubscriptionPlanDao _subscriptionPlanDao;
    private readonly IClaimContext _claimContext;
    private readonly IQrPaymentViewBuilder _viewBuilder;

    public SubscriptionQueryService(IPendingSubscriptionPaymentDao pendingSubscriptionPaymentDao,
                                    ISuccessSubscriptionPaymentDao successSubscriptionPaymentDao,
                                    ITodotixOutboxDao todotixOutboxDao,
                                    ISubscriptionPlanDao subscriptionPlanDao,
                                    IClaimContext claimContext,
                                    IQrPaymentViewBuilder viewBuilder)
    {
        _pendingSubscriptionPaymentDao = pendingSubscriptionPaymentDao;
        _successSubscriptionPaymentDao = successSubscriptionPaymentDao;
        _todotixOutboxDao = todotixOutboxDao;
        _subscriptionPlanDao = subscriptionPlanDao;
        _claimContext = claimContext;
        _viewBuilder = viewBuilder;
    }

    public async Task<GetQrDebtStatusOutcome> GetDebtStatusAsync(Guid paymentId)
    {
        Guid tenantId = _claimContext.TenantId;
        PendingSubscriptionPayment? pending = await _pendingSubscriptionPaymentDao.GetByIdForTenantAsync(tenantId, paymentId);
        if (pending != null)
        {
            if (!string.IsNullOrEmpty(pending.QrImageUrl))
            {
                return new GetQrDebtStatusOutcome.Found(_viewBuilder.BuildReadyStatus(pending.Id, pending.QrImageUrl));
            }

            TodotixOutboxEvent? outboxEvent = await _todotixOutboxDao.GetByPendingIdAsync(paymentId);
            if (outboxEvent != null && outboxEvent.Status == "Failed")
            {
                return new GetQrDebtStatusOutcome.Found(_viewBuilder.BuildFailedStatus(pending.Id, outboxEvent.LastError));
            }

            return new GetQrDebtStatusOutcome.Found(_viewBuilder.BuildPendingStatus(pending.Id));
        }

        SuccessSubscriptionPayment? success = await _successSubscriptionPaymentDao.GetByIdAsync(paymentId);
        if (success != null && success.TenantId == tenantId)
        {
            return new GetQrDebtStatusOutcome.Found(_viewBuilder.BuildReadyStatus(success.Id, null));
        }

        return new GetQrDebtStatusOutcome.NotFound();
    }

    public async Task<List<SubscriptionPlanDto>> ListPlansAsync()
    {
        List<SubscriptionPlan> plans = await _subscriptionPlanDao.GetAllAsync();
        return plans.ConvertAll(plan => new SubscriptionPlanDto
        {
            Level = plan.Level,
            Price = plan.Price,
            DurationAmount = plan.DurationAmount,
            DurationUnit = plan.DurationUnit
        });
    }
}
