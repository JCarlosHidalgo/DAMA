namespace Backend.Dtos.Courses.Output;

public class GetCourseDto : ICourseData
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }
}
