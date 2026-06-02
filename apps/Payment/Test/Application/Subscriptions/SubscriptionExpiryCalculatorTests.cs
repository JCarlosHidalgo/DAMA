using Backend.Application.Subscriptions;

namespace Test.Application.Subscriptions;

[TestFixture]
public class SubscriptionExpiryCalculatorTests
{
    private static readonly DateTime From = new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ComputeExpiryUtc_Days_AddsDays()
    {
        DateTime result = SubscriptionExpiryCalculator.ComputeExpiryUtc(From, 10, "Day");
        Assert.That(result, Is.EqualTo(From.AddDays(10)));
    }

    [Test]
    public void ComputeExpiryUtc_Weeks_AddsSevenDaysPerWeek()
    {
        DateTime result = SubscriptionExpiryCalculator.ComputeExpiryUtc(From, 3, "Week");
        Assert.That(result, Is.EqualTo(From.AddDays(21)));
    }

    [Test]
    public void ComputeExpiryUtc_Months_AddsCalendarMonths()
    {
        DateTime result = SubscriptionExpiryCalculator.ComputeExpiryUtc(From, 1, "month");
        Assert.That(result, Is.EqualTo(From.AddMonths(1)));
    }

    [Test]
    public void ComputeExpiryUtc_UnknownUnit_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SubscriptionExpiryCalculator.ComputeExpiryUtc(From, 1, "Fortnight"));
    }
}
