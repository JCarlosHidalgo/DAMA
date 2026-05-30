namespace Backend.Results.Users;

public abstract record RegisterUserOutcome
{
    private RegisterUserOutcome() { }

    public sealed record Created : RegisterUserOutcome;

    public sealed record DuplicateName : RegisterUserOutcome;
}
