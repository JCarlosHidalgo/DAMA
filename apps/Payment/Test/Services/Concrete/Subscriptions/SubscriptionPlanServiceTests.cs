using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Dtos.Subscriptions.Input;
using Backend.Entities.Subscriptions;
using Backend.Options;
using Backend.Services.Concrete.Subscriptions;

using Microsoft.Extensions.Options;

using Moq;

namespace Test.Services.Concrete.Subscriptions;

[TestFixture]
public class SubscriptionPlanServiceTests
{
    private Mock<ISubscriptionPlanDao> _subscriptionPlanDao = null!;
    private SubscriptionPlanService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _subscriptionPlanDao = new Mock<ISubscriptionPlanDao>(MockBehavior.Strict);
        IOptions<CurrencyOptions> currencyOptions = Options.Create(new CurrencyOptions { Default = "USD" });
        _sut = new SubscriptionPlanService(_subscriptionPlanDao.Object, currencyOptions);
    }

    [Test]
    public async Task UpdateAsync_BuildsPlanFromDtoAndCurrencyDefaultAndUpserts()
    {
        SubscriptionPlan? captured = null;
        _subscriptionPlanDao
            .Setup(d => d.UpsertAsync(It.IsAny<SubscriptionPlan>()))
            .Callback<SubscriptionPlan>(plan => captured = plan)
            .Returns(Task.CompletedTask);

        UpdateSubscriptionPlanDto dto = new()
        {
            Price = 250,
            DurationAmount = 3,
            DurationUnit = "Week"
        };

        await _sut.UpdateAsync(2, dto);

        Assert.That(captured, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(captured!.Level, Is.EqualTo(2));
            Assert.That(captured.Price, Is.EqualTo(250));
            Assert.That(captured.Currency, Is.EqualTo("USD"));
            Assert.That(captured.DurationAmount, Is.EqualTo(3));
            Assert.That(captured.DurationUnit, Is.EqualTo("Week"));
        });
        _subscriptionPlanDao.Verify(d => d.UpsertAsync(It.IsAny<SubscriptionPlan>()), Times.Once);
    }
}
