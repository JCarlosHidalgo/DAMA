using AutoMapper;

using Backend.Application.Mediator;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Dtos;
using Backend.Dtos.Uniques.Input;
using Backend.Entities;
using Backend.Entities.Uniques;
using Backend.Results.Uniques;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Application.Uniques;

public sealed record UpdateUniqueClassCommand(Guid UniqueClassId, UpdateUniqueClassDto Payload);

public sealed class UpdateUniqueClassHandler : ICommandHandler<UpdateUniqueClassCommand, UpdateUniqueClassResult>
{
    private readonly IUniqueClassDao _uniqueClassDao;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimContext _claimContext;
    private readonly IMapper _mapper;

    public UpdateUniqueClassHandler(IUniqueClassDao uniqueClassDao,
                                    IUnitOfWork unitOfWork,
                                    IClaimContext claimContext,
                                    IMapper mapper)
    {
        _uniqueClassDao = uniqueClassDao;
        _unitOfWork = unitOfWork;
        _claimContext = claimContext;
        _mapper = mapper;
    }

    public async Task<UpdateUniqueClassResult> Handle(UpdateUniqueClassCommand command)
    {
        Guid tenantId = _claimContext.TenantId;
        Guid uniqueClassId = command.UniqueClassId;
        UpdateUniqueClassDto payload = command.Payload;
        List<ClassTeacher> teachers = _mapper.Map<List<ClassTeacherDto>, List<ClassTeacher>>(payload.Teachers);

        UniqueClass? existing = await _uniqueClassDao.GetByIdForTenantAsync(tenantId, uniqueClassId);
        if (existing is null)
        {
            return new UpdateUniqueClassResult.NotFound();
        }

        if (await _uniqueClassDao.HasGroupOverlapAsync(tenantId, existing.GroupId, payload.Date, payload.StartTime, payload.EndTime, uniqueClassId))
        {
            return new UpdateUniqueClassResult.GroupOverlapConflict();
        }

        UniqueClassUpdate uniqueClassUpdate = new UniqueClassUpdate(
            uniqueClassId,
            payload.Date,
            payload.MaxStudentLimit,
            payload.StartTime,
            payload.EndTime);

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();
        bool updated = await _uniqueClassDao.UpdateForTenantAsync(uniqueClassUpdate, tenantId, scope);
        if (!updated)
        {
            return new UpdateUniqueClassResult.NotFound();
        }
        await _uniqueClassDao.ReplaceTeachersAsync(uniqueClassId, teachers, tenantId, scope);
        await scope.CommitAsync();
        return new UpdateUniqueClassResult.Updated();
    }
}
