using Backend.Application.Mediator;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.Entities;
using Backend.Results.Scheduleds;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Application.Scheduleds;

public sealed record DeleteScheduledClassCommand(Guid ScheduledClassId);

public sealed class DeleteScheduledClassHandler : ICommandHandler<DeleteScheduledClassCommand, DeleteScheduledClassResult>
{
    private readonly IScheduledClassDao _scheduledClassDao;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxEventDao _outboxEventDao;
    private readonly ICourseEventBuilder _courseEventBuilder;
    private readonly IClaimContext _claimContext;

    public DeleteScheduledClassHandler(
        IScheduledClassDao scheduledClassDao,
        IUnitOfWork unitOfWork,
        IOutboxEventDao outboxEventDao,
        ICourseEventBuilder courseEventBuilder,
        IClaimContext claimContext)
    {
        _scheduledClassDao = scheduledClassDao;
        _unitOfWork = unitOfWork;
        _outboxEventDao = outboxEventDao;
        _courseEventBuilder = courseEventBuilder;
        _claimContext = claimContext;
    }

    public async Task<DeleteScheduledClassResult> Handle(DeleteScheduledClassCommand command)
    {
        Guid tenantId = _claimContext.TenantId;

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();

        bool deleted = await _scheduledClassDao.DeleteForTenantAsync(tenantId, command.ScheduledClassId, scope);
        if (!deleted)
        {
            return new DeleteScheduledClassResult.NotFound();
        }

        OutboxEvent outboxEvent = _courseEventBuilder.BuildClassDeleted(tenantId, command.ScheduledClassId);
        await _outboxEventDao.InsertAsync(outboxEvent, scope);

        await scope.CommitAsync();
        return new DeleteScheduledClassResult.Deleted();
    }
}
