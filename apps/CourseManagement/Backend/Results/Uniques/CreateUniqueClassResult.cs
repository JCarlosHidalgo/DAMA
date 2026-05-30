using Backend.Dtos.Uniques.Output;

namespace Backend.Results.Uniques;

public abstract record CreateUniqueClassResult
{
    private CreateUniqueClassResult() { }

    public sealed record Created(GetUniqueClassDto UniqueClass) : CreateUniqueClassResult;

    public sealed record ReplayedFromIdempotency(GetUniqueClassDto UniqueClass) : CreateUniqueClassResult;

    public sealed record CourseNotFound : CreateUniqueClassResult;

    public sealed record TeacherConflict(Guid TeacherId, string TeacherName) : CreateUniqueClassResult;
}
