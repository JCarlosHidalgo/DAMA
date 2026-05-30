using AutoMapper;

using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.Dtos.Courses.Output;
using Backend.Entities.Courses;
using Backend.Results.Courses;

namespace Backend.Application.Courses;

public sealed record ListCoursesQuery;

public sealed class ListCoursesHandler : IQueryHandler<ListCoursesQuery, ListCoursesResult>
{
    private readonly ICourseDao _courseDao;
    private readonly IClaimContext _claimContext;
    private readonly IMapper _mapper;

    public ListCoursesHandler(ICourseDao courseDao, IClaimContext claimContext, IMapper mapper)
    {
        _courseDao = courseDao;
        _claimContext = claimContext;
        _mapper = mapper;
    }

    public async Task<ListCoursesResult> Handle(ListCoursesQuery query)
    {
        List<Course> courses = await _courseDao.GetCoursesByTenantIdAsync(_claimContext.TenantId);
        return new ListCoursesResult.Found(_mapper.Map<List<Course>, List<GetCourseDto>>(courses));
    }
}
