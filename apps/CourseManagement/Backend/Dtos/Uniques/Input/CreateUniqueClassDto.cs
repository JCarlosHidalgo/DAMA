namespace Backend.Dtos.Uniques.Input;

public class CreateUniqueClassDto : IUniqueClassPayload
{
    public required DateOnly Date { get; set; }

    public required int MaxStudentLimit { get; set; }

    public required TimeOnly StartTime { get; set; }

    public required TimeOnly EndTime { get; set; }

    public required Guid CourseId { get; set; }

    public required Guid GroupId { get; set; }

    public required List<ClassTeacherDto> Teachers { get; set; }

    public string? ExternalReference { get; set; }
}
