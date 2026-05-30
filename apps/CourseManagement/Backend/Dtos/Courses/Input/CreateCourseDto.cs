namespace Backend.Dtos.Courses.Input;

public class CreateCourseDto : ICourseData
{
    public required string Name { get; set; } = string.Empty;

    public string? ExternalReference { get; set; }
}
