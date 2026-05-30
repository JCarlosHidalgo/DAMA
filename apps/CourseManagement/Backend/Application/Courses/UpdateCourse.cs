using AutoMapper;

using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.Dtos.Courses.Input;
using Backend.Dtos.Courses.Output;
using Backend.Entities.Courses;
using Backend.Results.Courses;

namespace Backend.Application.Courses;

public sealed record UpdateCourseCommand(Guid CourseId, UpdateCourseDto Payload);

public sealed class UpdateCourseHandler : ICommandHandler<UpdateCourseCommand, UpdateCourseResult>
{
    private readonly ICourseDao _courseDao;
    private readonly IClaimContext _claimContext;
    private readonly IMapper _mapper;

    public UpdateCourseHandler(ICourseDao courseDao, IClaimContext claimContext, IMapper mapper)
    {
        _courseDao = courseDao;
        _claimContext = claimContext;
        _mapper = mapper;
    }

    public async Task<UpdateCourseResult> Handle(UpdateCourseCommand command)
    {
        Guid tenantId = _claimContext.TenantId;
        bool updated = await _courseDao.UpdateForTenantAsync(tenantId, command.CourseId, command.Payload.Name);
        if (!updated)
        {
            return new UpdateCourseResult.NotFound();
        }

        Course? course = await _courseDao.GetByIdForTenantAsync(tenantId, command.CourseId);
        if (course == null)
        {
            return new UpdateCourseResult.NotFound();
        }
        return new UpdateCourseResult.Updated(_mapper.Map<GetCourseDto>(course));
    }
}
