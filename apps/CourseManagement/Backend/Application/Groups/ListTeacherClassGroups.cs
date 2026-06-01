using AutoMapper;

using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.Dtos.Groups.Output;
using Backend.Entities.Groups;
using Backend.Results.Groups;

namespace Backend.Application.Groups;

public sealed record ListTeacherClassGroupsQuery;

public sealed class ListTeacherClassGroupsHandler : IQueryHandler<ListTeacherClassGroupsQuery, ListClassGroupsResult>
{
    private readonly IClassGroupDao _classGroupDao;
    private readonly IClaimContext _claimContext;
    private readonly IMapper _mapper;

    public ListTeacherClassGroupsHandler(IClassGroupDao classGroupDao, IClaimContext claimContext, IMapper mapper)
    {
        _classGroupDao = classGroupDao;
        _claimContext = claimContext;
        _mapper = mapper;
    }

    public async Task<ListClassGroupsResult> Handle(ListTeacherClassGroupsQuery query)
    {
        Guid tenantId = _claimContext.TenantId;
        Guid teacherId = _claimContext.UserId;
        List<ClassGroup> groups = await _classGroupDao.GetByTeacherForTenantAsync(tenantId, teacherId);
        return new ListClassGroupsResult.Found(_mapper.Map<List<ClassGroup>, List<GetClassGroupDto>>(groups));
    }
}
