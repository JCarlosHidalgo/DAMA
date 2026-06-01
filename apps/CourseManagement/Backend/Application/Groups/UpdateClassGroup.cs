using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.Dtos.Groups.Input;
using Backend.Results.Groups;

namespace Backend.Application.Groups;

public sealed record UpdateClassGroupCommand(Guid GroupId, UpdateClassGroupDto Payload);

public sealed class UpdateClassGroupHandler : ICommandHandler<UpdateClassGroupCommand, UpdateClassGroupResult>
{
    private readonly IClassGroupDao _classGroupDao;
    private readonly IClaimContext _claimContext;

    public UpdateClassGroupHandler(IClassGroupDao classGroupDao, IClaimContext claimContext)
    {
        _classGroupDao = classGroupDao;
        _claimContext = claimContext;
    }

    public async Task<UpdateClassGroupResult> Handle(UpdateClassGroupCommand command)
    {
        Guid tenantId = _claimContext.TenantId;
        bool updated = await _classGroupDao.UpdateForTenantAsync(tenantId, command.GroupId, command.Payload.Name);
        return updated
            ? new UpdateClassGroupResult.Updated()
            : new UpdateClassGroupResult.NotFound();
    }
}
