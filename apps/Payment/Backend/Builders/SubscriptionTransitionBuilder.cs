using Backend.Entities.Subscriptions;

namespace Backend.Builders;

public class SubscriptionTransitionBuilder : ISubscriptionTransitionBuilder
{
    public SuccessSubscriptionPayment BuildSuccessPayment(PendingSubscriptionPayment pendingPayment)
    {
        return new SuccessSubscriptionPayment
        {
            Id = pendingPayment.Id,
            TenantId = pendingPayment.TenantId,
            Level = pendingPayment.Level,
            Cost = pendingPayment.Cost,
            Currency = pendingPayment.Currency,
            PaidAt = DateTime.UtcNow
        };
    }

    public FailedSubscriptionPayment BuildFailedPayment(PendingSubscriptionPayment pendingPayment)
    {
        return new FailedSubscriptionPayment
        {
            Id = pendingPayment.Id,
            TenantId = pendingPayment.TenantId,
            Level = pendingPayment.Level,
            Cost = pendingPayment.Cost,
            Currency = pendingPayment.Currency,
            FailedAt = DateTime.UtcNow
        };
    }
}
