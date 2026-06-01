namespace Backend.Results.Todotix;

public abstract record UpdateTodotixAppKeyOutcome
{
    private UpdateTodotixAppKeyOutcome() { }

    public sealed record Updated : UpdateTodotixAppKeyOutcome;
}
