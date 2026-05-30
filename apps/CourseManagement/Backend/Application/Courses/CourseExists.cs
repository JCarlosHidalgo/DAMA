using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.Results.Courses;

namespace Backend.Application.Courses;

public sealed record CourseExistsQuery(Guid CourseId);

public sealed class CourseExistsHandler : IQueryHandler<CourseExistsQuery, CourseExistsResult>
{
    private readonly ICourseDao _courseDao;
    private readonly IClaimContext _claimContext;

    public CourseExistsHandler(ICourseDao courseDao, IClaimContext claimContext)
    {
        _courseDao = courseDao;
        _claimContext = claimContext;
    }

    public async Task<CourseExistsResult> Handle(CourseExistsQuery query)
    {
        bool exists = await _courseDao.ExistsForTenantAsync(_claimContext.TenantId, query.CourseId);
        return exists ? new CourseExistsResult.Exists() : new CourseExistsResult.DoesNotExist();
    }
}
