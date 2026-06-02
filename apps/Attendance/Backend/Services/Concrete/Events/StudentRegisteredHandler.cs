using Backend.DB.Daos.Abstract.Single.Events;
using Backend.DB.Daos.Abstract.Single.Remain;
using Backend.Events;
using Backend.Logging;
using Backend.Results.Events;
using Backend.Services.Abstract.Events;

using DAMA.Software.MySqlOutbox;
using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.Events;

public sealed class StudentRegisteredHandler : IStudentRegisteredHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessedEventDao _processedEventDao;
    private readonly IStudentRemainClassesDao _remainClassesDao;
    private readonly ILogger<StudentRegisteredHandler> _logger;

    public StudentRegisteredHandler(IUnitOfWork unitOfWork,
                                     IProcessedEventDao processedEventDao,
                                     IStudentRemainClassesDao remainClassesDao,
                                     ILogger<StudentRegisteredHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _processedEventDao = processedEventDao;
        _remainClassesDao = remainClassesDao;
        _logger = logger;
    }

    public async Task<StudentRegisteredOutcome> HandleAsync(StudentRegisteredEvent studentRegisteredEvent, CancellationToken cancellationToken)
    {
        try
        {
            return await IdempotentTransaction.RunAsync<StudentRegisteredOutcome>(
                _unitOfWork,
                _processedEventDao,
                studentRegisteredEvent.EventId,
                new StudentRegisteredOutcome.AlreadyProcessed(),
                async scope =>
                {
                    await _remainClassesDao.IncrementAsync(
                        studentRegisteredEvent.Data.TenantId,
                        studentRegisteredEvent.Data.StudentId,
                        delta: 0,
                        studentName: studentRegisteredEvent.Data.UserName,
                        transaction: scope);
                    return new StudentRegisteredOutcome.RemainCreated();
                });
        }
        catch (Exception handlerException)
        {
            LogEvents.EventHandlerFailed(_logger, handlerException, "StudentRegistered", studentRegisteredEvent.EventId);
            return new StudentRegisteredOutcome.Failed();
        }
    }
}
