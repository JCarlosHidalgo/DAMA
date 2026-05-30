namespace Backend.Results.Events;

public abstract record StudentRegisteredOutcome
{
    private StudentRegisteredOutcome() { }

    public sealed record RemainCreated : StudentRegisteredOutcome;

    public sealed record AlreadyProcessed : StudentRegisteredOutcome;

    public sealed record Failed : StudentRegisteredOutcome;
}
