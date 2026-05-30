using Backend.Entities.QrPayments;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single.QrPayments;

public interface IQrPaymentIdempotencyDao
{
    Task<bool> TryRecordAsync(QrPaymentIdempotency idempotencyRecord, ITransactionContext transaction);

    Task<QrPaymentIdempotency?> GetByExternalReferenceAsync(Guid tenantId, string externalReference);
}
