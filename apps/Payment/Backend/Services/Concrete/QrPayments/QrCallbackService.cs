using Backend.Builders;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities;
using Backend.Entities.QrPayments;
using Backend.Services.Abstract.QrPayments;
using Backend.Services.Abstract.Todotix;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.QrPayments;

public class QrCallbackService : IQrCallbackService
{
    private readonly IPendingQrPaymentDao _pendingQrPaymentDao;
    private readonly ISuccessQrPaymentDao _successQrPaymentDao;
    private readonly IFailedQrPaymentDao _failedQrPaymentDao;
    private readonly IOutboxEventDao _outboxEventDao;
    private readonly ITodotixClient _todotixClient;
    private readonly ITodotixAppKeyResolver _appKeyResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQrPaymentTransitionBuilder _transitionBuilder;

    public QrCallbackService(IPendingQrPaymentDao pendingQrPaymentDao,
                             ISuccessQrPaymentDao successQrPaymentDao,
                             IFailedQrPaymentDao failedQrPaymentDao,
                             IOutboxEventDao outboxEventDao,
                             ITodotixClient todotixClient,
                             ITodotixAppKeyResolver appKeyResolver,
                             IUnitOfWork unitOfWork,
                             IQrPaymentTransitionBuilder transitionBuilder)
    {
        _pendingQrPaymentDao = pendingQrPaymentDao;
        _successQrPaymentDao = successQrPaymentDao;
        _failedQrPaymentDao = failedQrPaymentDao;
        _outboxEventDao = outboxEventDao;
        _todotixClient = todotixClient;
        _appKeyResolver = appKeyResolver;
        _unitOfWork = unitOfWork;
        _transitionBuilder = transitionBuilder;
    }

    public async Task HandleCallbackAsync(Guid transactionId, int error, int cancelOrder)
    {
        PendingQrPayment? pending = await _pendingQrPaymentDao.GetByIdAsync(transactionId);
        if (pending == null)
        {
            return;
        }

        string appKey = await _appKeyResolver.ResolveAsync(pending.TenantId);
        TodotixDebtState debtState = await _todotixClient.ConsultDebtAsync(transactionId, appKey);

        if (debtState == TodotixDebtState.Paid)
        {
            await TransitionToSuccessAsync(pending);
        }
        else
        {
            await TransitionToFailedAsync(pending);
        }
    }

    private async Task TransitionToSuccessAsync(PendingQrPayment pending)
    {
        await _unitOfWork.RunInTransactionAsync(async transaction =>
        {
            SuccessQrPayment success = _transitionBuilder.BuildSuccessPayment(pending);
            await _successQrPaymentDao.TryCreateAsync(success, transaction);
            await _pendingQrPaymentDao.DeleteAsync(pending.Id, transaction);

            OutboxEvent capturedEvent = _transitionBuilder.BuildCapturedOutboxEvent(pending);
            await _outboxEventDao.InsertAsync(capturedEvent, transaction);
        });
    }

    private async Task TransitionToFailedAsync(PendingQrPayment pending)
    {
        await _unitOfWork.RunInTransactionAsync(async transaction =>
        {
            FailedQrPayment failed = _transitionBuilder.BuildFailedPayment(pending);
            await _failedQrPaymentDao.TryCreateAsync(failed, transaction);
            await _pendingQrPaymentDao.DeleteAsync(pending.Id, transaction);
        });
    }
}
