using Backend.Dtos.Scheduleds.Output;

namespace Backend.Results.Scheduleds;

public abstract record CreateScheduledClassResult
{
    private CreateScheduledClassResult() { }

    public sealed record Created(GetScheduledClassDto ScheduledClass) : CreateScheduledClassResult;

    public sealed record ReplayedFromIdempotency(GetScheduledClassDto ScheduledClass) : CreateScheduledClassResult;

    public sealed record CourseNotFound : CreateScheduledClassResult;

    public sealed record TeacherConflict(Guid TeacherId, string TeacherName) : CreateScheduledClassResult;
}
