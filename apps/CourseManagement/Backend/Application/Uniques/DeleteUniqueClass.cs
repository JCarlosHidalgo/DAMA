using Backend.Application.Mediator;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Entities;
using Backend.Results.Uniques;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Application.Uniques;

public sealed record DeleteUniqueClassCommand(Guid UniqueClassId);

public sealed class DeleteUniqueClassHandler : ICommandHandler<DeleteUniqueClassCommand, DeleteUniqueClassResult>
{
    private readonly IUniqueClassDao _uniqueClassDao;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxEventDao _outboxEventDao;
    private readonly ICourseEventBuilder _courseEventBuilder;
    private readonly IClaimContext _claimContext;

    public DeleteUniqueClassHandler(
        IUniqueClassDao uniqueClassDao,
        IUnitOfWork unitOfWork,
        IOutboxEventDao outboxEventDao,
        ICourseEventBuilder courseEventBuilder,
        IClaimContext claimContext)
    {
        _uniqueClassDao = uniqueClassDao;
        _unitOfWork = unitOfWork;
        _outboxEventDao = outboxEventDao;
        _courseEventBuilder = courseEventBuilder;
        _claimContext = claimContext;
    }

    public async Task<DeleteUniqueClassResult> Handle(DeleteUniqueClassCommand command)
    {
        Guid tenantId = _claimContext.TenantId;

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();

        bool deleted = await _uniqueClassDao.DeleteForTenantAsync(tenantId, command.UniqueClassId, scope);
        if (!deleted)
        {
            return new DeleteUniqueClassResult.NotFound();
        }

        OutboxEvent outboxEvent = _courseEventBuilder.BuildClassDeleted(tenantId, command.UniqueClassId);
        await _outboxEventDao.InsertAsync(outboxEvent, scope);

        await scope.CommitAsync();
        return new DeleteUniqueClassResult.Deleted();
    }
}
