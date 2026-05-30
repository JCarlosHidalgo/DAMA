using Backend.Dtos.Courses.Output;

namespace Backend.Results.Courses;

public abstract record ListCoursesResult
{
    private ListCoursesResult() { }

    public sealed record Found(List<GetCourseDto> Courses) : ListCoursesResult;
}
