using Backend.Builders;
using Backend.Entities.Subscriptions;

namespace Test.Builders;

[TestFixture]
public class SubscriptionTransitionBuilderTests
{
    private SubscriptionTransitionBuilder _sut = null!;

    [SetUp]
    public void Setup() => _sut = new SubscriptionTransitionBuilder();

    private static PendingSubscriptionPayment NewPending()
    {
        return new PendingSubscriptionPayment
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Level = 3,
            Cost = 300,
            Currency = "BOB",
            QrImageUrl = "http://qr",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
    }

    [Test]
    public void BuildSuccessPayment_MirrorsPendingFieldsAndStampsPaidAt()
    {
        PendingSubscriptionPayment pending = NewPending();
        DateTime before = DateTime.UtcNow;

        SuccessSubscriptionPayment success = _sut.BuildSuccessPayment(pending);

        Assert.Multiple(() =>
        {
            Assert.That(success.Id, Is.EqualTo(pending.Id));
            Assert.That(success.TenantId, Is.EqualTo(pending.TenantId));
            Assert.That(success.Level, Is.EqualTo(pending.Level));
            Assert.That(success.Cost, Is.EqualTo(pending.Cost));
            Assert.That(success.Currency, Is.EqualTo(pending.Currency));
            Assert.That(success.PaidAt, Is.GreaterThanOrEqualTo(before));
        });
    }

    [Test]
    public void BuildFailedPayment_MirrorsPendingFieldsAndStampsFailedAt()
    {
        PendingSubscriptionPayment pending = NewPending();
        DateTime before = DateTime.UtcNow;

        FailedSubscriptionPayment failed = _sut.BuildFailedPayment(pending);

        Assert.Multiple(() =>
        {
            Assert.That(failed.Id, Is.EqualTo(pending.Id));
            Assert.That(failed.TenantId, Is.EqualTo(pending.TenantId));
            Assert.That(failed.Level, Is.EqualTo(pending.Level));
            Assert.That(failed.Cost, Is.EqualTo(pending.Cost));
            Assert.That(failed.Currency, Is.EqualTo(pending.Currency));
            Assert.That(failed.FailedAt, Is.GreaterThanOrEqualTo(before));
        });
    }
}
