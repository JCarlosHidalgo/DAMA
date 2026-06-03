using Backend.Dtos.QrPayments.Output;

namespace Backend.Application.Results;

public abstract record CreateSubscriptionDebtOutcome
{
    private CreateSubscriptionDebtOutcome() { }

    public sealed record Success(QrDebtPendingDto Created) : CreateSubscriptionDebtOutcome;

    public sealed record PlanNotFound : CreateSubscriptionDebtOutcome;

    public sealed record PaymentNotConfigured : CreateSubscriptionDebtOutcome;
}
