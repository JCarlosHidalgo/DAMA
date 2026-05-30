using Backend.Dtos.Scheduleds.Output;
using Backend.Dtos.Uniques.Output;

namespace Backend.Dtos.Schedules.Output;

public class GetCourseScheduleDto
{
    public required List<GetScheduledClassDto> ScheduledClasses { get; set; }

    public required List<GetUniqueClassDto> UniqueClasses { get; set; }
}
