using AutoMapper;

using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.Dtos.Groups.Output;
using Backend.Entities.Groups;
using Backend.Results.Groups;

namespace Backend.Application.Groups;

public sealed record ListClassGroupsQuery;

public sealed class ListClassGroupsHandler : IQueryHandler<ListClassGroupsQuery, ListClassGroupsResult>
{
    private readonly IClassGroupDao _classGroupDao;
    private readonly IClaimContext _claimContext;
    private readonly IMapper _mapper;

    public ListClassGroupsHandler(IClassGroupDao classGroupDao, IClaimContext claimContext, IMapper mapper)
    {
        _classGroupDao = classGroupDao;
        _claimContext = claimContext;
        _mapper = mapper;
    }

    public async Task<ListClassGroupsResult> Handle(ListClassGroupsQuery query)
    {
        Guid tenantId = _claimContext.TenantId;
        List<ClassGroup> groups = await _classGroupDao.GetByTenantAsync(tenantId);
        return new ListClassGroupsResult.Found(_mapper.Map<List<ClassGroup>, List<GetClassGroupDto>>(groups));
    }
}
