using Backend.DB.Daos.Abstract.Single.Attendance;
using Backend.DB.Daos.Abstract.Single.Events;
using Backend.Events;
using Backend.Results.Events;
using Backend.Services.Abstract.Events;

using DAMA.Software.MySqlOutbox;
using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.Events;

public sealed class CourseDeletedHandler : ICourseDeletedHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessedEventDao _processedEventDao;
    private readonly IScheduledClassAttendanceDao _scheduledClassAttendanceDao;
    private readonly IUniqueClassAttendanceDao _uniqueClassAttendanceDao;
    private readonly ILogger<CourseDeletedHandler> _logger;

    public CourseDeletedHandler(
        IUnitOfWork unitOfWork,
        IProcessedEventDao processedEventDao,
        IScheduledClassAttendanceDao scheduledClassAttendanceDao,
        IUniqueClassAttendanceDao uniqueClassAttendanceDao,
        ILogger<CourseDeletedHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _processedEventDao = processedEventDao;
        _scheduledClassAttendanceDao = scheduledClassAttendanceDao;
        _uniqueClassAttendanceDao = uniqueClassAttendanceDao;
        _logger = logger;
    }

    public async Task<CourseDeletedOutcome> HandleAsync(CourseDeletedEvent courseDeletedEvent, CancellationToken cancellationToken)
    {
        try
        {
            return await IdempotentTransaction.RunAsync<CourseDeletedOutcome>(
                _unitOfWork,
                _processedEventDao,
                courseDeletedEvent.EventId,
                new CourseDeletedOutcome.AlreadyProcessed(),
                async scope =>
                {
                    foreach (Guid classId in courseDeletedEvent.Data.ClassIds)
                    {
                        await _scheduledClassAttendanceDao.DeleteByClassForTenantAsync(
                            courseDeletedEvent.Data.TenantId,
                            classId,
                            scope);
                        await _uniqueClassAttendanceDao.DeleteByClassForTenantAsync(
                            courseDeletedEvent.Data.TenantId,
                            classId,
                            scope);
                    }
                    return new CourseDeletedOutcome.AttendancesDeleted();
                });
        }
        catch (Exception handlerException)
        {
            _logger.LogError(
                handlerException,
                "Handle CourseDeleted falló para {EventId}",
                courseDeletedEvent.EventId);
            return new CourseDeletedOutcome.Failed();
        }
    }
}
