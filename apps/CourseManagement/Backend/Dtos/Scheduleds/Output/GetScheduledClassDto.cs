namespace Backend.Dtos.Scheduleds.Output;

public class GetScheduledClassDto : IScheduledClassPayload
{
    public required Guid Id { get; set; }

    public required int DayOfWeekIndex { get; set; }

    public required int MaxStudentLimit { get; set; }

    public required TimeOnly StartTime { get; set; }

    public required TimeOnly EndTime { get; set; }

    public required Guid CourseId { get; set; }

    public required List<ClassTeacherDto> Teachers { get; set; }
}
