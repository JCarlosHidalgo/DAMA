using System.Diagnostics;

using AutoMapper;

using Backend.Application.Infrastructure;
using Backend.Application.Mediator;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.Dtos;
using Backend.Dtos.Scheduleds.Input;
using Backend.Dtos.Scheduleds.Output;
using Backend.Entities;
using Backend.Entities.Scheduleds;
using Backend.Results.Scheduleds;

namespace Backend.Application.Scheduleds;

public sealed record CreateScheduledClassCommand(CreateScheduledClassDto Payload);

public sealed class CreateScheduledClassHandler : ICommandHandler<CreateScheduledClassCommand, CreateScheduledClassResult>
{
    private const string ScheduledClassEntityType = "ScheduledClass";

    private readonly IScheduledClassDao _scheduledClassDao;
    private readonly IClassCreationCoordinator<ScheduledClass> _coordinator;
    private readonly IClaimContext _claimContext;
    private readonly IClassBuilder _classBuilder;
    private readonly IMapper _mapper;

    public CreateScheduledClassHandler(IScheduledClassDao scheduledClassDao,
                                       IClassCreationCoordinator<ScheduledClass> coordinator,
                                       IClaimContext claimContext,
                                       IClassBuilder classBuilder,
                                       IMapper mapper)
    {
        _scheduledClassDao = scheduledClassDao;
        _coordinator = coordinator;
        _claimContext = claimContext;
        _classBuilder = classBuilder;
        _mapper = mapper;
    }

    public async Task<CreateScheduledClassResult> Handle(CreateScheduledClassCommand command)
    {
        Guid tenantId = _claimContext.TenantId;
        CreateScheduledClassDto payload = command.Payload;
        List<ClassTeacher> teachers = _mapper.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers);

        foreach (ClassTeacher teacher in teachers)
        {
            if (await _scheduledClassDao.HasOverlapForTeacherAsync(tenantId, teacher.TeacherId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, null))
            {
                return new CreateScheduledClassResult.TeacherConflict(teacher.TeacherId, teacher.TeacherName);
            }
        }

        ScheduledClass newScheduledClass = _classBuilder.BuildScheduledClass(tenantId, payload.CourseId, payload, teachers);

        ClassCreationOutcome<ScheduledClass> outcome = await _coordinator.CreateAsync(
            tenantId,
            payload.CourseId,
            payload.ExternalReference,
            ScheduledClassEntityType,
            newScheduledClass.Id,
            newScheduledClass,
            teachers);

        return outcome switch
        {
            ClassCreationOutcome<ScheduledClass>.Created created => new CreateScheduledClassResult.Created(_mapper.Map<GetScheduledClassDto>(created.Entity)),
            ClassCreationOutcome<ScheduledClass>.Replayed replayed => new CreateScheduledClassResult.ReplayedFromIdempotency(_mapper.Map<GetScheduledClassDto>(replayed.Prior)),
            ClassCreationOutcome<ScheduledClass>.CourseMissing => new CreateScheduledClassResult.CourseNotFound(),
            _ => throw new UnreachableException()
        };
    }
}
