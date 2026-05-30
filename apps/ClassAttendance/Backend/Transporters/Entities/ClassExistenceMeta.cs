namespace Backend.Transporters.Entities;

public record ClassExistenceMeta(
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly? ClassDate,
    int MaxStudentLimit
);
