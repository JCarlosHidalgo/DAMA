using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Entities.Uniques;
using Backend.Results.Uniques;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Application.Uniques;

public sealed record TransferUniqueClassCommand(Guid UniqueClassId, Guid TargetGroupId);

public sealed class TransferUniqueClassHandler : ICommandHandler<TransferUniqueClassCommand, TransferUniqueClassResult>
{
    private readonly IUniqueClassDao _uniqueClassDao;
    private readonly IClassGroupDao _classGroupDao;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimContext _claimContext;

    public TransferUniqueClassHandler(IUniqueClassDao uniqueClassDao,
                                      IClassGroupDao classGroupDao,
                                      IUnitOfWork unitOfWork,
                                      IClaimContext claimContext)
    {
        _uniqueClassDao = uniqueClassDao;
        _classGroupDao = classGroupDao;
        _unitOfWork = unitOfWork;
        _claimContext = claimContext;
    }

    public async Task<TransferUniqueClassResult> Handle(TransferUniqueClassCommand command)
    {
        Guid tenantId = _claimContext.TenantId;
        Guid uniqueClassId = command.UniqueClassId;
        Guid targetGroupId = command.TargetGroupId;

        UniqueClass? existing = await _uniqueClassDao.GetByIdForTenantAsync(tenantId, uniqueClassId);
        if (existing is null)
        {
            return new TransferUniqueClassResult.NotFound();
        }

        if (!await _classGroupDao.ExistsForTenantAsync(tenantId, targetGroupId))
        {
            return new TransferUniqueClassResult.GroupNotFound();
        }

        if (await _uniqueClassDao.HasGroupOverlapAsync(tenantId, targetGroupId, existing.Date, existing.StartTime, existing.EndTime, uniqueClassId))
        {
            return new TransferUniqueClassResult.GroupOverlapConflict();
        }

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();
        bool transferred = await _uniqueClassDao.TransferToGroupAsync(tenantId, uniqueClassId, targetGroupId, scope);
        if (!transferred)
        {
            return new TransferUniqueClassResult.NotFound();
        }
        await scope.CommitAsync();
        return new TransferUniqueClassResult.Transferred();
    }
}
