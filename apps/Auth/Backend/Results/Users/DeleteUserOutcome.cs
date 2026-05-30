namespace Backend.Results.Users;

public abstract record DeleteUserOutcome
{
    private DeleteUserOutcome() { }

    public sealed record Deleted : DeleteUserOutcome;

    public sealed record NotFound : DeleteUserOutcome;

    public sealed record SelfDeleteForbidden : DeleteUserOutcome;

    public sealed record ClientDeleteForbidden : DeleteUserOutcome;
}
