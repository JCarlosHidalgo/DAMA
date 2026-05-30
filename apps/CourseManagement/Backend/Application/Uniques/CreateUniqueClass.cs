using System.Diagnostics;

using AutoMapper;

using Backend.Application.Infrastructure;
using Backend.Application.Mediator;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Dtos;
using Backend.Dtos.Uniques.Input;
using Backend.Dtos.Uniques.Output;
using Backend.Entities;
using Backend.Entities.Uniques;
using Backend.Results.Uniques;

namespace Backend.Application.Uniques;

public sealed record CreateUniqueClassCommand(CreateUniqueClassDto Payload);

public sealed class CreateUniqueClassHandler : ICommandHandler<CreateUniqueClassCommand, CreateUniqueClassResult>
{
    private const string UniqueClassEntityType = "UniqueClass";

    private readonly IUniqueClassDao _uniqueClassDao;
    private readonly IClassCreationCoordinator<UniqueClass> _coordinator;
    private readonly IClaimContext _claimContext;
    private readonly IClassBuilder _classBuilder;
    private readonly IMapper _mapper;

    public CreateUniqueClassHandler(IUniqueClassDao uniqueClassDao,
                                    IClassCreationCoordinator<UniqueClass> coordinator,
                                    IClaimContext claimContext,
                                    IClassBuilder classBuilder,
                                    IMapper mapper)
    {
        _uniqueClassDao = uniqueClassDao;
        _coordinator = coordinator;
        _claimContext = claimContext;
        _classBuilder = classBuilder;
        _mapper = mapper;
    }

    public async Task<CreateUniqueClassResult> Handle(CreateUniqueClassCommand command)
    {
        Guid tenantId = _claimContext.TenantId;
        CreateUniqueClassDto payload = command.Payload;
        List<ClassTeacher> teachers = _mapper.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers);

        foreach (ClassTeacher teacher in teachers)
        {
            if (await _uniqueClassDao.HasOverlapForTeacherAsync(tenantId, teacher.TeacherId, payload.Date, payload.StartTime, payload.EndTime, null))
            {
                return new CreateUniqueClassResult.TeacherConflict(teacher.TeacherId, teacher.TeacherName);
            }
        }

        UniqueClass newUniqueClass = _classBuilder.BuildUniqueClass(tenantId, payload.CourseId, payload, teachers);

        ClassCreationOutcome<UniqueClass> outcome = await _coordinator.CreateAsync(
            tenantId,
            payload.CourseId,
            payload.ExternalReference,
            UniqueClassEntityType,
            newUniqueClass.Id,
            newUniqueClass,
            teachers);

        return outcome switch
        {
            ClassCreationOutcome<UniqueClass>.Created created => new CreateUniqueClassResult.Created(_mapper.Map<GetUniqueClassDto>(created.Entity)),
            ClassCreationOutcome<UniqueClass>.Replayed replayed => new CreateUniqueClassResult.ReplayedFromIdempotency(_mapper.Map<GetUniqueClassDto>(replayed.Prior)),
            ClassCreationOutcome<UniqueClass>.CourseMissing => new CreateUniqueClassResult.CourseNotFound(),
            _ => throw new UnreachableException()
        };
    }
}
