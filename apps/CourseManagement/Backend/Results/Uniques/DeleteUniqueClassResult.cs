namespace Backend.Results.Uniques;

public abstract record DeleteUniqueClassResult
{
    private DeleteUniqueClassResult() { }

    public sealed record Deleted : DeleteUniqueClassResult;

    public sealed record NotFound : DeleteUniqueClassResult;
}
