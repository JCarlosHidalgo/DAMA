using Backend.Builders;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities.QrPayments;
using Backend.Events;
using Backend.Results.QrPayments;
using Backend.Services.Abstract.Events;

using DAMA.Software.MySqlOutbox;
using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.Events;

public sealed class DebtExpiredHandler : IDebtExpiredHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessedEventDao _processedEventDao;
    private readonly IPendingQrPaymentDao _pendingQrPaymentDao;
    private readonly IFailedQrPaymentDao _failedQrPaymentDao;
    private readonly IQrPaymentTransitionBuilder _transitionBuilder;
    private readonly ILogger<DebtExpiredHandler> _logger;

    public DebtExpiredHandler(IUnitOfWork unitOfWork,
                              IProcessedEventDao processedEventDao,
                              IPendingQrPaymentDao pendingQrPaymentDao,
                              IFailedQrPaymentDao failedQrPaymentDao,
                              IQrPaymentTransitionBuilder transitionBuilder,
                              ILogger<DebtExpiredHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _processedEventDao = processedEventDao;
        _pendingQrPaymentDao = pendingQrPaymentDao;
        _failedQrPaymentDao = failedQrPaymentDao;
        _transitionBuilder = transitionBuilder;
        _logger = logger;
    }

    public async Task<HandleDebtExpiredOutcome> HandleAsync(DebtExpiredEvent debtExpiredEvent, CancellationToken cancellationToken)
    {
        try
        {
            return await IdempotentTransaction.RunAsync<HandleDebtExpiredOutcome>(
                _unitOfWork,
                _processedEventDao,
                debtExpiredEvent.EventId,
                new HandleDebtExpiredOutcome.AlreadyProcessed(),
                async scope =>
                {
                    PendingQrPayment? pending = await _pendingQrPaymentDao.GetByIdAsync(debtExpiredEvent.Data.PendingId);
                    if (pending == null)
                    {
                        return new HandleDebtExpiredOutcome.PendingMissing();
                    }

                    FailedQrPayment failed = _transitionBuilder.BuildFailedPayment(pending);
                    await _failedQrPaymentDao.TryCreateAsync(failed, scope);
                    await _pendingQrPaymentDao.DeleteAsync(pending.Id, scope);

                    return new HandleDebtExpiredOutcome.Processed();
                });
        }
        catch (Exception handlerException)
        {
            _logger.LogError(
                handlerException,
                "Handle DebtExpired falló para {EventId}",
                debtExpiredEvent.EventId);
            return new HandleDebtExpiredOutcome.Failed(handlerException.Message);
        }
    }
}
