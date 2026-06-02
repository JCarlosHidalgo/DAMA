using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Entities;
using Backend.Services.Abstract.Todotix;

namespace Backend.Services.Concrete.Todotix;

public sealed class SubscriptionQrImageUrlUpdater : IQrImageUrlUpdater
{
    private readonly IPendingSubscriptionPaymentDao _pendingSubscriptionPaymentDao;

    public SubscriptionQrImageUrlUpdater(IPendingSubscriptionPaymentDao pendingSubscriptionPaymentDao)
    {
        _pendingSubscriptionPaymentDao = pendingSubscriptionPaymentDao;
    }

    public DebtKind Kind => DebtKind.TenantSubscription;

    public Task UpdateAsync(Guid pendingId, string qrImageUrl)
    {
        return _pendingSubscriptionPaymentDao.UpdateQrImageUrlAsync(pendingId, qrImageUrl);
    }
}
