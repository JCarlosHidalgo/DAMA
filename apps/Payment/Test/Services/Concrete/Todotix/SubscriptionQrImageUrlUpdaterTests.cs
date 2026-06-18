using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Entities;
using Backend.Services.Concrete.Todotix;

using Moq;

namespace Test.Services.Concrete.Todotix;

[TestFixture]
public class SubscriptionQrImageUrlUpdaterTests
{
    private Mock<IPendingSubscriptionPaymentDao> _pendingSubscriptionPaymentDao = null!;
    private SubscriptionQrImageUrlUpdater _sut = null!;

    [SetUp]
    public void Setup()
    {
        _pendingSubscriptionPaymentDao = new Mock<IPendingSubscriptionPaymentDao>(MockBehavior.Strict);
        _sut = new SubscriptionQrImageUrlUpdater(_pendingSubscriptionPaymentDao.Object);
    }

    [Test]
    public void Kind_IsTenantSubscription()
    {
        Assert.That(_sut.Kind, Is.EqualTo(DebtKind.TenantSubscription));
    }

    [Test]
    public async Task UpdateAsync_DelegatesToDaoWithSameArguments()
    {
        var pendingId = Guid.NewGuid();
        const string qrImageUrl = "https://todotix.example.com/qr/image.png";
        _pendingSubscriptionPaymentDao.Setup(d => d.UpdateQrImageUrlAsync(pendingId, qrImageUrl)).Returns(Task.CompletedTask);

        await _sut.UpdateAsync(pendingId, qrImageUrl);

        _pendingSubscriptionPaymentDao.Verify(d => d.UpdateQrImageUrlAsync(pendingId, qrImageUrl), Times.Once);
    }
}
