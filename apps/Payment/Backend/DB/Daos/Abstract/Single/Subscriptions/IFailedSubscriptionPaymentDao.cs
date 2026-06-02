using Backend.Entities.Subscriptions;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single.Subscriptions;

public interface IFailedSubscriptionPaymentDao
{
    Task<bool> TryCreateAsync(FailedSubscriptionPayment payment, ITransactionContext transaction);
}
