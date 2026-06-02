using Backend.Entities.Subscriptions;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single.Subscriptions;

public interface IPendingSubscriptionPaymentDao
{
    Task CreateAsync(PendingSubscriptionPayment payment, ITransactionContext transaction);

    Task<PendingSubscriptionPayment?> GetByIdAsync(Guid paymentId);

    Task<PendingSubscriptionPayment?> GetByIdForTenantAsync(Guid tenantId, Guid paymentId);

    Task<int> CountActiveForTenantAsync(Guid tenantId, DateTime nowUtc);

    Task UpdateQrImageUrlAsync(Guid paymentId, string qrImageUrl);

    Task<bool> DeleteAsync(Guid paymentId, ITransactionContext transaction);
}
