using Backend.Entities.Subscriptions;

namespace Backend.Builders;

public interface ISubscriptionTransitionBuilder
{
    SuccessSubscriptionPayment BuildSuccessPayment(PendingSubscriptionPayment pendingPayment);

    FailedSubscriptionPayment BuildFailedPayment(PendingSubscriptionPayment pendingPayment);
}
