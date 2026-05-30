using AutoMapper;

using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.Dtos.Courses.Output;
using Backend.Entities.Courses;
using Backend.Results.Courses;

namespace Backend.Application.Courses;

public sealed record GetCourseByIdQuery(Guid CourseId);

public sealed class GetCourseByIdHandler : IQueryHandler<GetCourseByIdQuery, GetCourseByIdResult>
{
    private readonly ICourseDao _courseDao;
    private readonly IClaimContext _claimContext;
    private readonly IMapper _mapper;

    public GetCourseByIdHandler(ICourseDao courseDao, IClaimContext claimContext, IMapper mapper)
    {
        _courseDao = courseDao;
        _claimContext = claimContext;
        _mapper = mapper;
    }

    public async Task<GetCourseByIdResult> Handle(GetCourseByIdQuery query)
    {
        Course? course = await _courseDao.GetByIdForTenantAsync(_claimContext.TenantId, query.CourseId);
        if (course == null)
        {
            return new GetCourseByIdResult.NotFound();
        }
        return new GetCourseByIdResult.Found(_mapper.Map<GetCourseDto>(course));
    }
}
