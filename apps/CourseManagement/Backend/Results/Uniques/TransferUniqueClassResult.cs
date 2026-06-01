namespace Backend.Results.Uniques;

public abstract record TransferUniqueClassResult
{
    private TransferUniqueClassResult() { }

    public sealed record Transferred : TransferUniqueClassResult;

    public sealed record NotFound : TransferUniqueClassResult;

    public sealed record GroupNotFound : TransferUniqueClassResult;

    public sealed record GroupOverlapConflict : TransferUniqueClassResult;
}
