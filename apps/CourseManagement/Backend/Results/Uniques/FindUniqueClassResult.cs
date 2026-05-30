namespace Backend.Results.Uniques;

public abstract record FindUniqueClassResult
{
    private FindUniqueClassResult() { }

    public sealed record Found(ClassExistenceMeta Meta) : FindUniqueClassResult;

    public sealed record NotFound : FindUniqueClassResult;
}
