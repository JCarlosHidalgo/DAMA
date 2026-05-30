namespace Backend.Results.Scheduleds;

public abstract record DeleteScheduledClassResult
{
    private DeleteScheduledClassResult() { }

    public sealed record Deleted : DeleteScheduledClassResult;

    public sealed record NotFound : DeleteScheduledClassResult;
}
