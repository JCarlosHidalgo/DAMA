using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Dtos.Schedules.Output;
using Backend.Results.Schedules;

namespace Backend.Application.Schedules;

public sealed record GetTenantScheduleQuery(DateOnly ClassDatePointer);

public sealed class GetTenantScheduleHandler : IQueryHandler<GetTenantScheduleQuery, GetTenantScheduleResult>
{
    private readonly IScheduledClassDao _scheduledClassDao;
    private readonly IUniqueClassDao _uniqueClassDao;
    private readonly IScheduleAssembler _scheduleAssembler;
    private readonly IClaimContext _claimContext;

    public GetTenantScheduleHandler(IScheduledClassDao scheduledClassDao,
                                    IUniqueClassDao uniqueClassDao,
                                    IScheduleAssembler scheduleAssembler,
                                    IClaimContext claimContext)
    {
        _scheduledClassDao = scheduledClassDao;
        _uniqueClassDao = uniqueClassDao;
        _scheduleAssembler = scheduleAssembler;
        _claimContext = claimContext;
    }

    public async Task<GetTenantScheduleResult> Handle(GetTenantScheduleQuery query)
    {
        Guid tenantId = _claimContext.TenantId;
        GetCourseScheduleDto schedule = await _scheduleAssembler.AssembleAsync(
            () => _scheduledClassDao.GetByTenantAsync(tenantId),
            () => _uniqueClassDao.GetOnWeekForTenantAsync(tenantId, query.ClassDatePointer));
        return new GetTenantScheduleResult.Found(schedule);
    }
}
