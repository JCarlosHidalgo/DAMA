using Backend.Dtos;

namespace Backend.Dtos.Scheduleds;

public interface IScheduledClassPayload
{
    int DayOfWeekIndex { get; }

    int MaxStudentLimit { get; }

    TimeOnly StartTime { get; }

    TimeOnly EndTime { get; }

    List<ClassTeacherDto> Teachers { get; }
}
