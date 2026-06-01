namespace Backend.Results.Scheduleds;

public abstract record TransferScheduledClassResult
{
    private TransferScheduledClassResult() { }

    public sealed record Transferred : TransferScheduledClassResult;

    public sealed record NotFound : TransferScheduledClassResult;

    public sealed record GroupNotFound : TransferScheduledClassResult;

    public sealed record GroupOverlapConflict : TransferScheduledClassResult;
}
