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
            FailedAt = DateTime.UtcNow
        };
    }
}
