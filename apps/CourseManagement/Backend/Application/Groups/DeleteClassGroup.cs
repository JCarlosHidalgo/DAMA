using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.Results.Groups;

namespace Backend.Application.Groups;

public sealed record DeleteClassGroupCommand(Guid GroupId);

public sealed class DeleteClassGroupHandler : ICommandHandler<DeleteClassGroupCommand, DeleteClassGroupResult>
{
    private readonly IClassGroupDao _classGroupDao;
    private readonly IClaimContext _claimContext;

    public DeleteClassGroupHandler(IClassGroupDao classGroupDao, IClaimContext claimContext)
    {
        _classGroupDao = classGroupDao;
        _claimContext = claimContext;
    }

    public async Task<DeleteClassGroupResult> Handle(DeleteClassGroupCommand command)
    {
        Guid tenantId = _claimContext.TenantId;

        if (!await _classGroupDao.ExistsForTenantAsync(tenantId, command.GroupId))
        {
            return new DeleteClassGroupResult.NotFound();
        }

        bool deleted = await _classGroupDao.DeleteForTenantIfEmptyAsync(tenantId, command.GroupId);
        return deleted
            ? new DeleteClassGroupResult.Deleted()
            : new DeleteClassGroupResult.GroupNotEmpty();
    }
}
