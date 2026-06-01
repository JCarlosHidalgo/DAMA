namespace Backend.Results.Groups;

public abstract record UpdateClassGroupResult
{
    private UpdateClassGroupResult() { }

    public sealed record Updated : UpdateClassGroupResult;

    public sealed record NotFound : UpdateClassGroupResult;
}
