namespace Backend.Results.Events;

public abstract record PaymentCapturedOutcome
{
    private PaymentCapturedOutcome() { }

    public sealed record RemainCredited : PaymentCapturedOutcome;

    public sealed record AlreadyProcessed : PaymentCapturedOutcome;

    public sealed record Failed : PaymentCapturedOutcome;
}
