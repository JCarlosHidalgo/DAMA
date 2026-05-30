namespace Backend.Results.Courses;

public abstract record DeleteCourseResult
{
    private DeleteCourseResult() { }

    public sealed record Deleted : DeleteCourseResult;

    public sealed record NotFound : DeleteCourseResult;
}
