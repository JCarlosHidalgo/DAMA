namespace Backend.Results;

public record ClassExistenceMeta(
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly? ClassDate,
    int MaxStudentLimit
);
