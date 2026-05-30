namespace Backend.Results.Todotix;

public abstract record PublishOutcome
{
    private PublishOutcome() { }

    public sealed record Success : PublishOutcome;

    public sealed record TransientFailure(string Reason) : PublishOutcome;

    public sealed record PermanentFailure(string Reason) : PublishOutcome;
}
