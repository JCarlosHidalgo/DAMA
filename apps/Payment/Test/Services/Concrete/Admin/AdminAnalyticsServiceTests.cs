using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Dtos.Admin.Output;
using Backend.Options;
using Backend.Services.Concrete.Admin;

using Microsoft.Extensions.Options;

using Moq;

namespace Test.Services.Concrete.Admin;

[TestFixture]
public class AdminAnalyticsServiceTests
{
    private Mock<IAdminSubscriptionAnalyticsDao> _analyticsDao = null!;
    private IOptions<CurrencyOptions> _currencyOptions = null!;
    private AdminAnalyticsService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _analyticsDao = new Mock<IAdminSubscriptionAnalyticsDao>(MockBehavior.Strict);
        _currencyOptions = Options.Create(new CurrencyOptions { Default = "BOB" });
        _sut = new AdminAnalyticsService(_analyticsDao.Object, _currencyOptions);
    }

    [Test]
    public async Task GetRevenueTotalAsync_MapsRowAndStampsCurrency()
    {
        _analyticsDao.Setup(d => d.GetRevenueTotalAsync())
                     .ReturnsAsync(new SubscriptionRevenueTotalRow(1500, 9));

        SubscriptionRevenueTotalDto total = await _sut.GetRevenueTotalAsync();

        Assert.Multiple(() =>
        {
            Assert.That(total.TotalRevenue, Is.EqualTo(1500));
            Assert.That(total.PaymentCount, Is.EqualTo(9));
            Assert.That(total.Currency, Is.EqualTo("BOB"));
        });
    }

    [Test]
    public async Task GetRevenueTimelineAsync_MapsRowsToPoints()
    {
        DateTime from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime to = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        _analyticsDao.Setup(d => d.GetRevenueByMonthAsync(from, to))
                     .ReturnsAsync(new List<SubscriptionRevenueMonthRow>
                     {
                         new(2026, 1, 800, 4),
                         new(2026, 2, 700, 5)
                     });

        List<SubscriptionRevenuePointDto> timeline = await _sut.GetRevenueTimelineAsync(from, to);

        Assert.Multiple(() =>
        {
            Assert.That(timeline, Has.Count.EqualTo(2));
            Assert.That(timeline[0].Year, Is.EqualTo(2026));
            Assert.That(timeline[0].Month, Is.EqualTo(1));
            Assert.That(timeline[0].Amount, Is.EqualTo(800));
            Assert.That(timeline[0].Count, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task GetRevenueByTierAsync_MapsRows()
    {
        _analyticsDao.Setup(d => d.GetRevenueByTierAsync())
                     .ReturnsAsync(new List<SubscriptionRevenueTierRow>
                     {
                         new(1, 300, 3),
                         new(2, 1200, 6)
                     });

        List<SubscriptionRevenueByTierDto> byTier = await _sut.GetRevenueByTierAsync();

        Assert.Multiple(() =>
        {
            Assert.That(byTier, Has.Count.EqualTo(2));
            Assert.That(byTier[1].Level, Is.EqualTo(2));
            Assert.That(byTier[1].Revenue, Is.EqualTo(1200));
            Assert.That(byTier[1].Count, Is.EqualTo(6));
        });
    }
}
