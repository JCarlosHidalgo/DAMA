using AutoMapper;

using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.Dtos;
using Backend.Dtos.Scheduleds.Input;
using Backend.Entities;
using Backend.Entities.Scheduleds;
using Backend.Results.Scheduleds;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Application.Scheduleds;

public sealed record UpdateScheduledClassCommand(Guid ScheduledClassId, UpdateScheduledClassDto Payload);

public sealed class UpdateScheduledClassHandler : ICommandHandler<UpdateScheduledClassCommand, UpdateScheduledClassResult>
{
    private readonly IScheduledClassDao _scheduledClassDao;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimContext _claimContext;
    private readonly IMapper _mapper;

    public UpdateScheduledClassHandler(IScheduledClassDao scheduledClassDao,
                                       IUnitOfWork unitOfWork,
                                       IClaimContext claimContext,
                                       IMapper mapper)
    {
        _scheduledClassDao = scheduledClassDao;
        _unitOfWork = unitOfWork;
        _claimContext = claimContext;
        _mapper = mapper;
    }

    public async Task<UpdateScheduledClassResult> Handle(UpdateScheduledClassCommand command)
    {
        Guid tenantId = _claimContext.TenantId;
        Guid scheduledClassId = command.ScheduledClassId;
        UpdateScheduledClassDto payload = command.Payload;
        List<ClassTeacher> teachers = _mapper.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers);

        ScheduledClass? existing = await _scheduledClassDao.GetByIdForTenantAsync(tenantId, scheduledClassId);
        if (existing is null)
        {
            return new UpdateScheduledClassResult.NotFound();
        }

        if (await _scheduledClassDao.HasGroupOverlapAsync(tenantId, existing.GroupId, payload.DayOfWeekIndex, payload.StartTime, payload.EndTime, scheduledClassId))
        {
            return new UpdateScheduledClassResult.GroupOverlapConflict();
        }

        ScheduledClassUpdate scheduledClassUpdate = new ScheduledClassUpdate(
            scheduledClassId,
            payload.DayOfWeekIndex,
            payload.MaxStudentLimit,
            payload.StartTime,
            payload.EndTime);

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();
        bool updated = await _scheduledClassDao.UpdateForTenantAsync(scheduledClassUpdate, tenantId, scope);
        if (!updated)
        {
            return new UpdateScheduledClassResult.NotFound();
        }
        await _scheduledClassDao.ReplaceTeachersAsync(scheduledClassId, teachers, tenantId, scope);
        await scope.CommitAsync();
        return new UpdateScheduledClassResult.Updated();
    }
}
