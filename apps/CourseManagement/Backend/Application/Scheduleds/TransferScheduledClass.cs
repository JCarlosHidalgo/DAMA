using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.Entities.Scheduleds;
using Backend.Results.Scheduleds;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Application.Scheduleds;

public sealed record TransferScheduledClassCommand(Guid ScheduledClassId, Guid TargetGroupId);

public sealed class TransferScheduledClassHandler : ICommandHandler<TransferScheduledClassCommand, TransferScheduledClassResult>
{
    private readonly IScheduledClassDao _scheduledClassDao;
    private readonly IClassGroupDao _classGroupDao;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimContext _claimContext;

    public TransferScheduledClassHandler(IScheduledClassDao scheduledClassDao,
                                         IClassGroupDao classGroupDao,
                                         IUnitOfWork unitOfWork,
                                         IClaimContext claimContext)
    {
        _scheduledClassDao = scheduledClassDao;
        _classGroupDao = classGroupDao;
        _unitOfWork = unitOfWork;
        _claimContext = claimContext;
    }

    public async Task<TransferScheduledClassResult> Handle(TransferScheduledClassCommand command)
    {
        Guid tenantId = _claimContext.TenantId;
        Guid scheduledClassId = command.ScheduledClassId;
        Guid targetGroupId = command.TargetGroupId;

        ScheduledClass? existing = await _scheduledClassDao.GetByIdForTenantAsync(tenantId, scheduledClassId);
        if (existing is null)
        {
            return new TransferScheduledClassResult.NotFound();
        }

        if (!await _classGroupDao.ExistsForTenantAsync(tenantId, targetGroupId))
        {
            return new TransferScheduledClassResult.GroupNotFound();
        }

        if (await _scheduledClassDao.HasGroupOverlapAsync(tenantId, targetGroupId, existing.DayOfWeekIndex, existing.StartTime, existing.EndTime, scheduledClassId))
        {
            return new TransferScheduledClassResult.GroupOverlapConflict();
        }

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();
        bool transferred = await _scheduledClassDao.TransferToGroupAsync(tenantId, scheduledClassId, targetGroupId, scope);
        if (!transferred)
        {
            return new TransferScheduledClassResult.NotFound();
        }
        await scope.CommitAsync();
        return new TransferScheduledClassResult.Transferred();
    }
}
