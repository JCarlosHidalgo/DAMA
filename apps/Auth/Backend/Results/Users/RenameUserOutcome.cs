namespace Backend.Results.Users;

public abstract record RenameUserOutcome
{
    private RenameUserOutcome() { }

    public sealed record Renamed : RenameUserOutcome;

    public sealed record NotFound : RenameUserOutcome;

    public sealed record DuplicateName : RenameUserOutcome;
}
