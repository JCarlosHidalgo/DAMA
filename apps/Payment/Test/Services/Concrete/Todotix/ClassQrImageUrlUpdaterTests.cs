using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities;
using Backend.Services.Concrete.Todotix;

using Moq;

namespace Test.Services.Concrete.Todotix;

[TestFixture]
public class ClassQrImageUrlUpdaterTests
{
    private Mock<IPendingQrPaymentDao> _pendingQrPaymentDao = null!;
    private ClassQrImageUrlUpdater _sut = null!;

    [SetUp]
    public void Setup()
    {
        _pendingQrPaymentDao = new Mock<IPendingQrPaymentDao>(MockBehavior.Strict);
        _sut = new ClassQrImageUrlUpdater(_pendingQrPaymentDao.Object);
    }

    [Test]
    public void Kind_IsClassPurchase()
    {
        Assert.That(_sut.Kind, Is.EqualTo(DebtKind.ClassPurchase));
    }

    [Test]
    public async Task UpdateAsync_DelegatesToDaoWithSameArguments()
    {
        var pendingId = Guid.NewGuid();
        const string qrImageUrl = "https://todotix.example.com/qr/image.png";
        _pendingQrPaymentDao.Setup(d => d.UpdateQrImageUrlAsync(pendingId, qrImageUrl)).Returns(Task.CompletedTask);

        await _sut.UpdateAsync(pendingId, qrImageUrl);

        _pendingQrPaymentDao.Verify(d => d.UpdateQrImageUrlAsync(pendingId, qrImageUrl), Times.Once);
    }
}
