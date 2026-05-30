using AutoMapper;

using Backend.Dtos.Scheduleds.Output;
using Backend.Dtos.Schedules.Output;
using Backend.Dtos.Uniques.Output;
using Backend.Entities.Scheduleds;
using Backend.Entities.Uniques;

namespace Backend.Application.Schedules;

public interface IScheduleAssembler
{
    Task<GetCourseScheduleDto> AssembleAsync(
        Func<Task<List<ScheduledClass>>> loadScheduled,
        Func<Task<List<UniqueClass>>> loadUnique);
}

public sealed class ScheduleAssembler : IScheduleAssembler
{
    private readonly IMapper _mapper;

    public ScheduleAssembler(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task<GetCourseScheduleDto> AssembleAsync(
        Func<Task<List<ScheduledClass>>> loadScheduled,
        Func<Task<List<UniqueClass>>> loadUnique)
    {
        List<ScheduledClass> scheduledClasses = await loadScheduled();
        List<UniqueClass> uniqueClasses = await loadUnique();

        return new GetCourseScheduleDto
        {
            ScheduledClasses = _mapper.Map<List<ScheduledClass>, List<GetScheduledClassDto>>(scheduledClasses),
            UniqueClasses = _mapper.Map<List<UniqueClass>, List<GetUniqueClassDto>>(uniqueClasses)
        };
    }
}
