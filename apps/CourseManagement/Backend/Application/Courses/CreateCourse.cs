using System.Diagnostics;

using AutoMapper;

using Backend.Application.Infrastructure;
using Backend.Application.Mediator;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.Dtos.Courses.Input;
using Backend.Dtos.Courses.Output;
using Backend.Entities.Courses;
using Backend.Results.Courses;

namespace Backend.Application.Courses;

public sealed record CreateCourseCommand(CreateCourseDto Payload);

public sealed class CreateCourseHandler : ICommandHandler<CreateCourseCommand, CreateCourseResult>
{
    private const string CourseEntityType = "Course";

    private readonly ICourseDao _courseDao;
    private readonly IIdempotentTransactionExecutor _idempotentExecutor;
    private readonly IClaimContext _claimContext;
    private readonly ICourseBuilder _courseBuilder;
    private readonly IMapper _mapper;

    public CreateCourseHandler(ICourseDao courseDao,
                               IIdempotentTransactionExecutor idempotentExecutor,
                               IClaimContext claimContext,
                               ICourseBuilder courseBuilder,
                               IMapper mapper)
    {
        _courseDao = courseDao;
        _idempotentExecutor = idempotentExecutor;
        _claimContext = claimContext;
        _courseBuilder = courseBuilder;
        _mapper = mapper;
    }

    public async Task<CreateCourseResult> Handle(CreateCourseCommand command)
    {
        Guid tenantId = _claimContext.TenantId;
        Course candidate = _courseBuilder.BuildCourse(tenantId, command.Payload);

        IdempotentInsertOutcome<Course> outcome = await _idempotentExecutor.ExecuteAsync<Course>(
            tenantId,
            command.Payload.ExternalReference,
            CourseEntityType,
            candidate.Id,
            async transaction =>
            {
                await _courseDao.CreateAsync(candidate, transaction);
                return candidate;
            },
            entityId => _courseDao.GetByIdForTenantAsync(tenantId, entityId));

        return outcome switch
        {
            IdempotentInsertOutcome<Course>.Inserted inserted => new CreateCourseResult.Created(_mapper.Map<GetCourseDto>(inserted.Entity)),
            IdempotentInsertOutcome<Course>.Replayed replayed => new CreateCourseResult.ReplayedFromIdempotency(_mapper.Map<GetCourseDto>(replayed.Prior)),
            _ => throw new UnreachableException("Course create has no FK parent that could fail.")
        };
    }
}
