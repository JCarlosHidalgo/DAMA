using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Dtos.Schedules.Output;
using Backend.Results.Schedules;

namespace Backend.Application.Schedules;

public sealed record GetTeacherScheduleQuery(DateOnly ClassDatePointer);

public sealed class GetTeacherScheduleHandler : IQueryHandler<GetTeacherScheduleQuery, GetTeacherScheduleResult>
{
    private readonly IScheduledClassDao _scheduledClassDao;
    private readonly IUniqueClassDao _uniqueClassDao;
    private readonly IScheduleAssembler _scheduleAssembler;
    private readonly IClaimContext _claimContext;

    public GetTeacherScheduleHandler(IScheduledClassDao scheduledClassDao,
                                     IUniqueClassDao uniqueClassDao,
                                     IScheduleAssembler scheduleAssembler,
                                     IClaimContext claimContext)
    {
        _scheduledClassDao = scheduledClassDao;
        _uniqueClassDao = uniqueClassDao;
        _scheduleAssembler = scheduleAssembler;
        _claimContext = claimContext;
    }

    public async Task<GetTeacherScheduleResult> Handle(GetTeacherScheduleQuery query)
    {
        Guid tenantId = _claimContext.TenantId;
        Guid teacherId = _claimContext.UserId;
        GetCourseScheduleDto schedule = await _scheduleAssembler.AssembleAsync(
            () => _scheduledClassDao.GetByTeacherForTenantAsync(tenantId, teacherId),
            () => _uniqueClassDao.GetByTeacherOnWeekForTenantAsync(tenantId, teacherId, query.ClassDatePointer));
        return new GetTeacherScheduleResult.Found(schedule);
    }
}
