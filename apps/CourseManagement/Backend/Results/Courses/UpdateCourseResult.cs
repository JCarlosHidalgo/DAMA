using Backend.Dtos.Courses.Output;

namespace Backend.Results.Courses;

public abstract record UpdateCourseResult
{
    private UpdateCourseResult() { }

    public sealed record Updated(GetCourseDto Course) : UpdateCourseResult;

    public sealed record NotFound : UpdateCourseResult;
}
