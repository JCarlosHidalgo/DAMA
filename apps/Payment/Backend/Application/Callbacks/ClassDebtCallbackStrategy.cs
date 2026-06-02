using Backend.Builders;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities;
using Backend.Entities.QrPayments;
using Backend.Services.Abstract.Todotix;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Application.Callbacks;

public sealed class ClassDebtCallbackStrategy : DebtCallbackStrategyBase<PendingQrPayment>
{
    private readonly IPendingQrPaymentDao _pendingQrPaymentDao;
    private readonly ISuccessQrPaymentDao _successQrPaymentDao;
    private readonly IFailedQrPaymentDao _failedQrPaymentDao;
    private readonly IOutboxEventDao _outboxEventDao;
    private readonly ITodotixAppKeyResolver _appKeyResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQrPaymentTransitionBuilder _transitionBuilder;

    public ClassDebtCallbackStrategy(IPendingQrPaymentDao pendingQrPaymentDao,
                                     ISuccessQrPaymentDao successQrPaymentDao,
                                     IFailedQrPaymentDao failedQrPaymentDao,
                                     IOutboxEventDao outboxEventDao,
                                     ITodotixClient todotixClient,
                                     ITodotixAppKeyResolver appKeyResolver,
                                     IUnitOfWork unitOfWork,
                                     IQrPaymentTransitionBuilder transitionBuilder)
        : base(todotixClient)
    {
        _pendingQrPaymentDao = pendingQrPaymentDao;
        _successQrPaymentDao = successQrPaymentDao;
        _failedQrPaymentDao = failedQrPaymentDao;
        _outboxEventDao = outboxEventDao;
        _appKeyResolver = appKeyResolver;
        _unitOfWork = unitOfWork;
        _transitionBuilder = transitionBuilder;
    }

    public override DebtKind Kind => DebtKind.ClassPurchase;

    protected override async Task<PendingQrPayment?> LoadPendingAsync(Guid transactionId)
    {
        return await _pendingQrPaymentDao.GetByIdAsync(transactionId);
    }

    protected override async Task<string?> ResolveAppKeyAsync(PendingQrPayment pending)
    {
        return await _appKeyResolver.ResolveAsync(pending.TenantId);
    }

    protected override async Task TransitionToSuccessAsync(PendingQrPayment pending)
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

    protected override async Task TransitionToFailedAsync(PendingQrPayment pending)
    {
        await _unitOfWork.RunInTransactionAsync(async transaction =>
        {
            FailedQrPayment failed = _transitionBuilder.BuildFailedPayment(pending);
            await _failedQrPaymentDao.TryCreateAsync(failed, transaction);
            await _pendingQrPaymentDao.DeleteAsync(pending.Id, transaction);
        });
    }
}
