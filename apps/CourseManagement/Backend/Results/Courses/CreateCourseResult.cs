using Backend.Dtos.Courses.Output;

namespace Backend.Results.Courses;

public abstract record CreateCourseResult
{
    private CreateCourseResult() { }

    public sealed record Created(GetCourseDto Course) : CreateCourseResult;

    public sealed record ReplayedFromIdempotency(GetCourseDto Course) : CreateCourseResult;
}
