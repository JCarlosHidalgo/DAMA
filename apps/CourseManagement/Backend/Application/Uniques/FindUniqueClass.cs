using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Results;
using Backend.Results.Uniques;

namespace Backend.Application.Uniques;

public sealed record FindUniqueClassQuery(Guid ClassId);

public sealed class FindUniqueClassHandler : IQueryHandler<FindUniqueClassQuery, FindUniqueClassResult>
{
    private readonly IUniqueClassDao _uniqueClassDao;
    private readonly IClaimContext _claimContext;

    public FindUniqueClassHandler(IUniqueClassDao uniqueClassDao, IClaimContext claimContext)
    {
        _uniqueClassDao = uniqueClassDao;
        _claimContext = claimContext;
    }

    public async Task<FindUniqueClassResult> Handle(FindUniqueClassQuery query)
    {
        ClassExistenceMeta? meta = await _uniqueClassDao.FindForTenantAsync(_claimContext.TenantId, query.ClassId);
        if (meta == null)
        {
            return new FindUniqueClassResult.NotFound();
        }
        return new FindUniqueClassResult.Found(meta);
    }
}
