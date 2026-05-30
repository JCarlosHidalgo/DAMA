namespace Backend.Entities.Scheduleds;

public sealed record ScheduledClassUpdate(
    Guid Id,
    int DayOfWeekIndex,
    int MaxStudentLimit,
    TimeOnly StartTime,
    TimeOnly EndTime);
