using Backend.Dtos;

namespace Backend.Dtos.Uniques;

public interface IUniqueClassPayload
{
    DateOnly Date { get; }

    int MaxStudentLimit { get; }

    TimeOnly StartTime { get; }

    TimeOnly EndTime { get; }

    List<ClassTeacherDto> Teachers { get; }
}
