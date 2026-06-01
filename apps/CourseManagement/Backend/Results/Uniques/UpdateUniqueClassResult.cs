namespace Backend.Results.Uniques;

public abstract record UpdateUniqueClassResult
{
    private UpdateUniqueClassResult() { }

    public sealed record Updated : UpdateUniqueClassResult;

    public sealed record NotFound : UpdateUniqueClassResult;

    public sealed record GroupOverlapConflict : UpdateUniqueClassResult;
}
