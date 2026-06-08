using Backend.Entities;
using Backend.Entities.QrPayments;

namespace Backend.Builders;

public interface IQrPaymentTransitionBuilder
{
    OutboxEvent BuildCapturedOutboxEvent(PendingQrPayment pendingPayment);

    SuccessQrPayment BuildSuccessPayment(PendingQrPayment pendingPayment);

    FailedQrPayment BuildFailedPayment(PendingQrPayment pendingPayment, FailureReason reason);
}
