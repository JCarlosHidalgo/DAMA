namespace Backend.Results.QrPayments;

public abstract record HandleDebtExpiredOutcome
{
    private HandleDebtExpiredOutcome() { }

    public sealed record Processed : HandleDebtExpiredOutcome;

    public sealed record AlreadyProcessed : HandleDebtExpiredOutcome;

    public sealed record PendingMissing : HandleDebtExpiredOutcome;

    public sealed record Failed(string Reason) : HandleDebtExpiredOutcome;
}
