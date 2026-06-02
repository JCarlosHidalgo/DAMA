using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities;
using Backend.Services.Abstract.Todotix;

namespace Backend.Services.Concrete.Todotix;

public sealed class ClassQrImageUrlUpdater : IQrImageUrlUpdater
{
    private readonly IPendingQrPaymentDao _pendingQrPaymentDao;

    public ClassQrImageUrlUpdater(IPendingQrPaymentDao pendingQrPaymentDao)
    {
        _pendingQrPaymentDao = pendingQrPaymentDao;
    }

    public DebtKind Kind => DebtKind.ClassPurchase;

    public Task UpdateAsync(Guid pendingId, string qrImageUrl)
    {
        return _pendingQrPaymentDao.UpdateQrImageUrlAsync(pendingId, qrImageUrl);
    }
}
