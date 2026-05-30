using Backend.Application.Mediator;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Dtos.Schedules.Output;
using Backend.Results.Schedules;

namespace Backend.Application.Schedules;

public sealed record GetCourseScheduleQuery(Guid CourseId, DateOnly CurrentDate);

public sealed class GetCourseScheduleHandler : IQueryHandler<GetCourseScheduleQuery, GetCourseScheduleResult>
{
    private readonly IScheduledClassDao _scheduledClassDao;
    private readonly IUniqueClassDao _uniqueClassDao;
    private readonly IScheduleAssembler _scheduleAssembler;

    public GetCourseScheduleHandler(IScheduledClassDao scheduledClassDao,
                                    IUniqueClassDao uniqueClassDao,
                                    IScheduleAssembler scheduleAssembler)
    {
        _scheduledClassDao = scheduledClassDao;
        _uniqueClassDao = uniqueClassDao;
        _scheduleAssembler = scheduleAssembler;
    }

    public async Task<GetCourseScheduleResult> Handle(GetCourseScheduleQuery query)
    {
        GetCourseScheduleDto schedule = await _scheduleAssembler.AssembleAsync(
            () => _scheduledClassDao.GetScheduledClassesByCourseIdAsync(query.CourseId),
            () => _uniqueClassDao.GetUniqueClassesOnSameWeekByDateAsync(query.CourseId, query.CurrentDate));
        return new GetCourseScheduleResult.Found(schedule);
    }
}
