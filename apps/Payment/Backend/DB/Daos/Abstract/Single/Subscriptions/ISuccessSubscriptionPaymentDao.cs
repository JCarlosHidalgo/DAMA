using Backend.Entities.Subscriptions;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single.Subscriptions;

public interface ISuccessSubscriptionPaymentDao
{
    Task<bool> TryCreateAsync(SuccessSubscriptionPayment payment, ITransactionContext transaction);

    Task<SuccessSubscriptionPayment?> GetByIdForTenantAsync(Guid tenantId, Guid paymentId);
}
