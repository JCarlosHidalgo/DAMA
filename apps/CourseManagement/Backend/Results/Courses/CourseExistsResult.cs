namespace Backend.Results.Courses;

public abstract record CourseExistsResult
{
    private CourseExistsResult() { }

    public sealed record Exists : CourseExistsResult;

    public sealed record DoesNotExist : CourseExistsResult;
}
