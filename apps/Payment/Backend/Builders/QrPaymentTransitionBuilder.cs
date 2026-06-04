using System.Text.Json;

using Backend.Entities;
using Backend.Entities.QrPayments;
using Backend.Events;

namespace Backend.Builders;

public class QrPaymentTransitionBuilder : IQrPaymentTransitionBuilder
{
    public OutboxEvent BuildCapturedOutboxEvent(PendingQrPayment pendingPayment)
    {
        Guid eventId = pendingPayment.Id;
        DateTime occurredAt = DateTime.UtcNow;

        PaymentCapturedEvent domainEvent = new PaymentCapturedEvent
        {
            EventId = eventId,
            EventType = "PaymentCaptured",
            OccurredAt = occurredAt,
            AggregateId = pendingPayment.Id,
            Data = new PaymentCapturedData(
                pendingPayment.TenantId,
                pendingPayment.StudentId,
                pendingPayment.ClassQuantity,
                pendingPayment.Id.ToString("D"))
        };

        return new OutboxEvent
        {
            Id = eventId,
            AggregateType = "Payment",
            AggregateId = pendingPayment.Id,
            EventType = "PaymentCaptured",
            RoutingKey = "payment.captured",
            Payload = JsonSerializer.Serialize(domainEvent),
            OccurredAt = occurredAt
        };
    }

    public SuccessQrPayment BuildSuccessPayment(PendingQrPayment pendingPayment)
    {
        return new SuccessQrPayment
        {
            Id = pendingPayment.Id,
            TenantId = pendingPayment.TenantId,
            StudentId = pendingPayment.StudentId,
            ClassQuantity = pendingPayment.ClassQuantity,
            Cost = pendingPayment.Cost,
            Currency = pendingPayment.Currency,
            PaidAt = DateTime.UtcNow
        };
    }

    public FailedQrPayment BuildFailedPayment(PendingQrPayment pendingPayment)
    {
        return new FailedQrPayment
        {
            Id = pendingPayment.Id,
            TenantId = pendingPayment.TenantId,
            StudentId = pendingPayment.StudentId,
            ClassQuantity = pendingPayment.ClassQuantity,
            Cost = pendingPayment.Cost,
            Currency = pendingPayment.Currency,
            FailedAt = DateTime.UtcNow
        };
    }
}
