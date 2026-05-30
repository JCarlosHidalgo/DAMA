namespace Backend.Results.Scheduleds;

public abstract record UpdateScheduledClassResult
{
    private UpdateScheduledClassResult() { }

    public sealed record Updated : UpdateScheduledClassResult;

    public sealed record NotFound : UpdateScheduledClassResult;

    public sealed record TeacherConflict(Guid TeacherId, string TeacherName) : UpdateScheduledClassResult;
}
