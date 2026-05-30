using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.Results;
using Backend.Results.Scheduleds;

namespace Backend.Application.Scheduleds;

public sealed record FindScheduledClassQuery(Guid ClassId, DateOnly ClassDate);

public sealed class FindScheduledClassHandler : IQueryHandler<FindScheduledClassQuery, FindScheduledClassResult>
{
    private readonly IScheduledClassDao _scheduledClassDao;
    private readonly IClaimContext _claimContext;

    public FindScheduledClassHandler(IScheduledClassDao scheduledClassDao, IClaimContext claimContext)
    {
        _scheduledClassDao = scheduledClassDao;
        _claimContext = claimContext;
    }

    public async Task<FindScheduledClassResult> Handle(FindScheduledClassQuery query)
    {
        ClassExistenceMeta? meta = await _scheduledClassDao.FindForTenantAsync(_claimContext.TenantId, query.ClassId, query.ClassDate);
        if (meta == null)
        {
            return new FindScheduledClassResult.NotFound();
        }
        return new FindScheduledClassResult.Found(meta);
    }
}
