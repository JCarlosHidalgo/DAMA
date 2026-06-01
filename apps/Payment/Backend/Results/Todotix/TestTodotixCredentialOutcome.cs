namespace Backend.Results.Todotix;

public abstract record TestTodotixCredentialOutcome
{
    private TestTodotixCredentialOutcome() { }

    public sealed record Works : TestTodotixCredentialOutcome;

    public sealed record NotConfigured : TestTodotixCredentialOutcome;

    public sealed record Failed : TestTodotixCredentialOutcome;
}
