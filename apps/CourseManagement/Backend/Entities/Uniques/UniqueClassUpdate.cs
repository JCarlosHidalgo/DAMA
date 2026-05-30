namespace Backend.Entities.Uniques;

public sealed record UniqueClassUpdate(
    Guid Id,
    DateOnly Date,
    int MaxStudentLimit,
    TimeOnly StartTime,
    TimeOnly EndTime);
