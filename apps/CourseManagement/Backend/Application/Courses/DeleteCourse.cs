using Backend.Application.Mediator;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Entities;
using Backend.Results.Courses;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Application.Courses;

public sealed record DeleteCourseCommand(Guid CourseId);

public sealed class DeleteCourseHandler : ICommandHandler<DeleteCourseCommand, DeleteCourseResult>
{
    private readonly ICourseDao _courseDao;
    private readonly IScheduledClassDao _scheduledClassDao;
    private readonly IUniqueClassDao _uniqueClassDao;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxEventDao _outboxEventDao;
    private readonly ICourseEventBuilder _courseEventBuilder;
    private readonly IClaimContext _claimContext;

    public DeleteCourseHandler(
        ICourseDao courseDao,
        IScheduledClassDao scheduledClassDao,
        IUniqueClassDao uniqueClassDao,
        IUnitOfWork unitOfWork,
        IOutboxEventDao outboxEventDao,
        ICourseEventBuilder courseEventBuilder,
        IClaimContext claimContext)
    {
        _courseDao = courseDao;
        _scheduledClassDao = scheduledClassDao;
        _uniqueClassDao = uniqueClassDao;
        _unitOfWork = unitOfWork;
        _outboxEventDao = outboxEventDao;
        _courseEventBuilder = courseEventBuilder;
        _claimContext = claimContext;
    }

    public async Task<DeleteCourseResult> Handle(DeleteCourseCommand command)
    {
        Guid tenantId = _claimContext.TenantId;

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();

        List<Guid> scheduledClassIds = await _scheduledClassDao.GetIdsByCourseForTenantAsync(tenantId, command.CourseId, scope);
        List<Guid> uniqueClassIds = await _uniqueClassDao.GetIdsByCourseForTenantAsync(tenantId, command.CourseId, scope);

        bool deleted = await _courseDao.DeleteForTenantAsync(tenantId, command.CourseId, scope);
        if (!deleted)
        {
            return new DeleteCourseResult.NotFound();
        }

        List<Guid> affectedClassIds = new List<Guid>(scheduledClassIds.Count + uniqueClassIds.Count);
        affectedClassIds.AddRange(scheduledClassIds);
        affectedClassIds.AddRange(uniqueClassIds);

        OutboxEvent outboxEvent = _courseEventBuilder.BuildCourseDeleted(tenantId, command.CourseId, affectedClassIds);
        await _outboxEventDao.InsertAsync(outboxEvent, scope);

        await scope.CommitAsync();
        return new DeleteCourseResult.Deleted();
    }
}
