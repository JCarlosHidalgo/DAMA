namespace Backend.Dtos.Scheduleds.Input;

public class CreateScheduledClassDto : IScheduledClassPayload
{
    public required int DayOfWeekIndex { get; set; }

    public required int MaxStudentLimit { get; set; }

    public required TimeOnly StartTime { get; set; }

    public required TimeOnly EndTime { get; set; }

    public required Guid CourseId { get; set; }

    public required List<ClassTeacherDto> Teachers { get; set; }

    public string? ExternalReference { get; set; }
}
