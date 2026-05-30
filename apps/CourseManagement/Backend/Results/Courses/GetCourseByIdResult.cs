using Backend.Dtos.Courses.Output;

namespace Backend.Results.Courses;

public abstract record GetCourseByIdResult
{
    private GetCourseByIdResult() { }

    public sealed record Found(GetCourseDto Course) : GetCourseByIdResult;

    public sealed record NotFound : GetCourseByIdResult;
}
