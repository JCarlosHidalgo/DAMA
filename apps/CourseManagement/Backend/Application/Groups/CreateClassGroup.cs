using AutoMapper;

using Backend.Application.Mediator;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.Dtos.Groups.Input;
using Backend.Dtos.Groups.Output;
using Backend.Entities.Groups;
using Backend.Results.Groups;

namespace Backend.Application.Groups;

public sealed record CreateClassGroupCommand(CreateClassGroupDto Payload);

public sealed class CreateClassGroupHandler : ICommandHandler<CreateClassGroupCommand, CreateClassGroupResult>
{
    private readonly IClassGroupDao _classGroupDao;
    private readonly IClassGroupBuilder _classGroupBuilder;
    private readonly IClaimContext _claimContext;
    private readonly IMapper _mapper;

    public CreateClassGroupHandler(IClassGroupDao classGroupDao,
                                   IClassGroupBuilder classGroupBuilder,
                                   IClaimContext claimContext,
                                   IMapper mapper)
    {
        _classGroupDao = classGroupDao;
        _classGroupBuilder = classGroupBuilder;
        _claimContext = claimContext;
        _mapper = mapper;
    }

    public async Task<CreateClassGroupResult> Handle(CreateClassGroupCommand command)
    {
        Guid tenantId = _claimContext.TenantId;
        ClassGroup classGroup = _classGroupBuilder.BuildClassGroup(tenantId, command.Payload);
        await _classGroupDao.CreateForTenantAsync(classGroup);
        return new CreateClassGroupResult.Created(_mapper.Map<GetClassGroupDto>(classGroup));
    }
}
