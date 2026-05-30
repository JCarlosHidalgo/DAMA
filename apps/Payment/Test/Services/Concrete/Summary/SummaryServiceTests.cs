using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Dtos.Summary.Output;
using Backend.Services.Concrete.Summary;

using Moq;

namespace Test.Services.Concrete.Summary;

[TestFixture]
public class SummaryServiceTests
{
    private Mock<ISuccessQrPaymentDao> successDao = null!;
    private Mock<IClaimContext> claimContext = null!;
    private SummaryService sut = null!;
    private Guid tenantId;

    [SetUp]
    public void Setup()
    {
        successDao = new Mock<ISuccessQrPaymentDao>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        tenantId = Guid.NewGuid();
        claimContext.Setup(c => c.TenantId).Returns(tenantId);
        sut = new SummaryService(successDao.Object, claimContext.Object);
    }

    [Test]
    public async Task GetByTenantAsync_WithData_ReturnsTotalsAndDates()
    {
        DateTime firstPaid = DateTime.UtcNow.AddDays(-100);
        successDao.Setup(d => d.GetSummaryAsync(tenantId, It.IsAny<DateTime>()))
                  .ReturnsAsync((1000, 200, (DateTime?)firstPaid));

        PaymentSummaryDto summary = await sut.GetByTenantAsync();

        Assert.Multiple(() =>
        {
            Assert.That(summary.TotalEarnings, Is.EqualTo(1000));
            Assert.That(summary.MonthEarnings, Is.EqualTo(200));
            Assert.That(summary.FirstPaymentDate, Is.EqualTo(firstPaid));
            Assert.That(summary.To, Is.GreaterThanOrEqualTo(summary.From));
            Assert.That((summary.To - summary.From).TotalDays, Is.EqualTo(30).Within(0.001));
        });
    }

    [Test]
    public async Task GetByTenantAsync_WithoutData_ReturnsZeroEarningsAndNullFirstPaid()
    {
        successDao.Setup(d => d.GetSummaryAsync(tenantId, It.IsAny<DateTime>()))
                  .ReturnsAsync((0, 0, (DateTime?)null));

        PaymentSummaryDto summary = await sut.GetByTenantAsync();

        Assert.Multiple(() =>
        {
            Assert.That(summary.TotalEarnings, Is.EqualTo(0));
            Assert.That(summary.MonthEarnings, Is.EqualTo(0));
            Assert.That(summary.FirstPaymentDate, Is.Null);
        });
    }
}
