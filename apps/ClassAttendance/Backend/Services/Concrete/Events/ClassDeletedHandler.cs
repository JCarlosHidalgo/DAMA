using Backend.DB.Daos.Abstract.Single.Attendance;
using Backend.DB.Daos.Abstract.Single.Events;
using Backend.Events;
using Backend.Results.Events;
using Backend.Services.Abstract.Events;

using DAMA.Software.MySqlOutbox;
using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.Events;

public sealed class ClassDeletedHandler : IClassDeletedHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessedEventDao _processedEventDao;
    private readonly IScheduledClassAttendanceDao _scheduledClassAttendanceDao;
    private readonly IUniqueClassAttendanceDao _uniqueClassAttendanceDao;
    private readonly ILogger<ClassDeletedHandler> _logger;

    public ClassDeletedHandler(
        IUnitOfWork unitOfWork,
        IProcessedEventDao processedEventDao,
        IScheduledClassAttendanceDao scheduledClassAttendanceDao,
        IUniqueClassAttendanceDao uniqueClassAttendanceDao,
        ILogger<ClassDeletedHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _processedEventDao = processedEventDao;
        _scheduledClassAttendanceDao = scheduledClassAttendanceDao;
        _uniqueClassAttendanceDao = uniqueClassAttendanceDao;
        _logger = logger;
    }

    public async Task<ClassDeletedOutcome> HandleAsync(ClassDeletedEvent classDeletedEvent, CancellationToken cancellationToken)
    {
        try
        {
            return await IdempotentTransaction.RunAsync<ClassDeletedOutcome>(
                _unitOfWork,
                _processedEventDao,
                classDeletedEvent.EventId,
                new ClassDeletedOutcome.AlreadyProcessed(),
                async scope =>
                {
                    await _scheduledClassAttendanceDao.DeleteByClassForTenantAsync(
                        classDeletedEvent.Data.TenantId,
                        classDeletedEvent.Data.ClassId,
                        scope);
                    await _uniqueClassAttendanceDao.DeleteByClassForTenantAsync(
                        classDeletedEvent.Data.TenantId,
                        classDeletedEvent.Data.ClassId,
                        scope);
                    return new ClassDeletedOutcome.AttendancesDeleted();
                });
        }
        catch (Exception handlerException)
        {
            _logger.LogError(
                handlerException,
                "Handle ClassDeleted falló para {EventId}",
                classDeletedEvent.EventId);
            return new ClassDeletedOutcome.Failed();
        }
    }
}
