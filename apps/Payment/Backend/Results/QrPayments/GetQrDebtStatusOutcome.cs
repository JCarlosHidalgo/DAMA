using Backend.Dtos.QrPayments.Output;

namespace Backend.Results.QrPayments;

public abstract record GetQrDebtStatusOutcome
{
    private GetQrDebtStatusOutcome() { }

    public sealed record Found(QrDebtStatusDto Status) : GetQrDebtStatusOutcome;

    public sealed record NotFound : GetQrDebtStatusOutcome;
}
