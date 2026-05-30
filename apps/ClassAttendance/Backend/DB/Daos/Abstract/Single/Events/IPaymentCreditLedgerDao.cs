using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single.Events;

public interface IPaymentCreditLedgerDao
{
    Task RecordAsync(Guid eventId, Guid tenantId, Guid studentId, int quantity, string externalReference, DateTime occurredAt, ITransactionContext transaction);
}
