using Backend.Dtos.QrPayments.Output;

namespace Backend.Results.QrPayments;

public abstract record CreateQrDebtOutcome
{
    private CreateQrDebtOutcome() { }

    public sealed record Success(QrDebtPendingDto Created) : CreateQrDebtOutcome;

    public sealed record TemplateNotFound : CreateQrDebtOutcome;

    public sealed record ActiveDebtForTemplate : CreateQrDebtOutcome;
}
