namespace Backend.Results.Scheduleds;

public abstract record FindScheduledClassResult
{
    private FindScheduledClassResult() { }

    public sealed record Found(ClassExistenceMeta Meta) : FindScheduledClassResult;

    public sealed record NotFound : FindScheduledClassResult;
}
