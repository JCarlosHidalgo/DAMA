using Backend.Entities.QrPayments;

namespace Backend.DB.Daos.Abstract.Single.QrPayments;

public interface IPaymentCallbackInboxDao
{
    Task<bool> TryEnqueueAsync(Guid transactionId, int error, int cancelOrder);

    Task<List<PaymentCallback>> LeasePendingAsync(int batchSize, TimeSpan leaseDuration);

    Task MarkProcessedAsync(Guid id);

    Task RecordFailureAsync(Guid id, string error);

    Task MarkFailedAsync(Guid id, string error);
}
