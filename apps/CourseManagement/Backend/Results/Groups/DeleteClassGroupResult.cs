namespace Backend.Results.Groups;

public abstract record DeleteClassGroupResult
{
    private DeleteClassGroupResult() { }

    public sealed record Deleted : DeleteClassGroupResult;

    public sealed record NotFound : DeleteClassGroupResult;

    public sealed record GroupNotEmpty : DeleteClassGroupResult;
}
