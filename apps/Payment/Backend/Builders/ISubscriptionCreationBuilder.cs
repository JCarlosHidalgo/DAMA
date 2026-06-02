using Backend.Dtos.External.Todotix;
using Backend.Dtos.QrPayments.Output;
using Backend.Entities.Subscriptions;
using Backend.Entities.Todotix;

namespace Backend.Builders;

public interface ISubscriptionCreationBuilder
{
    PendingSubscriptionPayment BuildPendingPayment(Guid debtIdentifier, Guid tenantId, SubscriptionPlan plan, DateTime expiresAtUtc);

    RegisterDebtRequest BuildTodotixRequest(Guid debtIdentifier, string? email, SubscriptionPlan plan, string tenantTimezone, string description, DateTime expiresAtUtc, string appKey);

    TodotixOutboxEvent BuildOutboxEvent(Guid debtIdentifier, Guid tenantId, RegisterDebtRequest todotixRequest);

    QrDebtPendingDto BuildPendingDebtDto(Guid debtIdentifier);
}
