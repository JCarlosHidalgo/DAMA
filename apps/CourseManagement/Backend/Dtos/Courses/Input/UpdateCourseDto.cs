namespace Backend.Dtos.Courses.Input;

public class UpdateCourseDto : ICourseData
{
    public required string Name { get; set; } = string.Empty;
}
