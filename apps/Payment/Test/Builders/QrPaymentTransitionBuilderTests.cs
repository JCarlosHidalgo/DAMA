using System.Text.Json;

using Backend.Builders;
using Backend.Entities;
using Backend.Entities.QrPayments;
using Backend.Events;

namespace Test.Builders;

[TestFixture]
public class QrPaymentTransitionBuilderTests
{
    private QrPaymentTransitionBuilder sut = null!;

    [SetUp]
    public void Setup() => sut = new QrPaymentTransitionBuilder();

    private static PendingQrPayment NewPending()
    {
        return new PendingQrPayment
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            TemplateId = Guid.NewGuid(),
            ClassQuantity = 8,
            Cost = 800,
            QrImageUrl = "http://qr",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
    }

    [Test]
    public void BuildCapturedOutboxEvent_ReusesPendingIdAsEventId()
    {
        PendingQrPayment pending = NewPending();
        DateTime before = DateTime.UtcNow;

        OutboxEvent outboxEvent = sut.BuildCapturedOutboxEvent(pending);

        Assert.Multiple(() =>
        {
            Assert.That(outboxEvent.Id, Is.EqualTo(pending.Id));
            Assert.That(outboxEvent.AggregateType, Is.EqualTo("Payment"));
            Assert.That(outboxEvent.AggregateId, Is.EqualTo(pending.Id));
            Assert.That(outboxEvent.EventType, Is.EqualTo("PaymentCaptured"));
            Assert.That(outboxEvent.RoutingKey, Is.EqualTo("payment.captured"));
            Assert.That(outboxEvent.OccurredAt, Is.GreaterThanOrEqualTo(before));
            Assert.That(outboxEvent.Payload, Is.Not.Empty);
        });

        PaymentCapturedEvent? deserialized = JsonSerializer.Deserialize<PaymentCapturedEvent>(outboxEvent.Payload);
        Assert.That(deserialized, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(deserialized!.EventId, Is.EqualTo(pending.Id));
            Assert.That(deserialized.Data.TenantId, Is.EqualTo(pending.TenantId));
            Assert.That(deserialized.Data.StudentId, Is.EqualTo(pending.StudentId));
            Assert.That(deserialized.Data.Quantity, Is.EqualTo(pending.ClassQuantity));
            Assert.That(deserialized.Data.ExternalReference, Is.EqualTo(pending.Id.ToString("D")));
        });
    }

    [Test]
    public void BuildSuccessPayment_MirrorsPendingFieldsAndStampsPaidAt()
    {
        PendingQrPayment pending = NewPending();
        DateTime before = DateTime.UtcNow;

        SuccessQrPayment success = sut.BuildSuccessPayment(pending);

        Assert.Multiple(() =>
        {
            Assert.That(success.Id, Is.EqualTo(pending.Id));
            Assert.That(success.TenantId, Is.EqualTo(pending.TenantId));
            Assert.That(success.StudentId, Is.EqualTo(pending.StudentId));
            Assert.That(success.ClassQuantity, Is.EqualTo(pending.ClassQuantity));
            Assert.That(success.Cost, Is.EqualTo(pending.Cost));
            Assert.That(success.PaidAt, Is.GreaterThanOrEqualTo(before));
        });
    }

    [Test]
    public void BuildFailedPayment_MirrorsPendingFieldsAndStampsFailedAt()
    {
        PendingQrPayment pending = NewPending();
        DateTime before = DateTime.UtcNow;

        FailedQrPayment failed = sut.BuildFailedPayment(pending);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Id, Is.EqualTo(pending.Id));
            Assert.That(failed.TenantId, Is.EqualTo(pending.TenantId));
            Assert.That(failed.StudentId, Is.EqualTo(pending.StudentId));
            Assert.That(failed.ClassQuantity, Is.EqualTo(pending.ClassQuantity));
            Assert.That(failed.Cost, Is.EqualTo(pending.Cost));
            Assert.That(failed.FailedAt, Is.GreaterThanOrEqualTo(before));
        });
    }
}
