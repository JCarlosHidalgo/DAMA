using Backend.Application.Schedules;

namespace Test.Application.Schedules;

[TestFixture]
public class WeekResolverTests
{
    [Test]
    public void TenantToday_ReturnsTenantLocalDate_WhenUtcIsAlreadyOnTheNextDay()
    {
        DateTime utcNow = new(2026, 6, 2, 3, 14, 0, DateTimeKind.Utc);

        DateOnly today = WeekResolver.TenantToday("America/La_Paz", utcNow);

        Assert.That(today, Is.EqualTo(new DateOnly(2026, 6, 1)));
    }

    [Test]
    public void TenantToday_FallsBackToUtcDate_WhenTimezoneIsInvalid()
    {
        DateTime utcNow = new(2026, 6, 2, 3, 14, 0, DateTimeKind.Utc);

        DateOnly today = WeekResolver.TenantToday("Not/AZone", utcNow);

        Assert.That(today, Is.EqualTo(new DateOnly(2026, 6, 2)));
    }

    [Test]
    public void ResolveWeek_SnapsToMonday_ForAMidWeekDay()
    {
        DateOnly wednesday = new(2026, 6, 3);

        (DateOnly pointer, DateOnly weekStart) = WeekResolver.ResolveWeek(wednesday, 0);

        Assert.Multiple(() =>
        {
            Assert.That(pointer, Is.EqualTo(wednesday));
            Assert.That(weekStart, Is.EqualTo(new DateOnly(2026, 6, 1)));
        });
    }

    [Test]
    public void ResolveWeek_SnapsToMonday_WhenTodayIsSunday()
    {
        DateOnly sunday = new(2026, 6, 7);

        (DateOnly pointer, DateOnly weekStart) = WeekResolver.ResolveWeek(sunday, 0);

        Assert.Multiple(() =>
        {
            Assert.That(pointer, Is.EqualTo(sunday));
            Assert.That(weekStart, Is.EqualTo(new DateOnly(2026, 6, 1)));
        });
    }

    [Test]
    public void ResolveWeek_ShiftsByWholeWeeks_ForNonZeroPaginationIndex()
    {
        DateOnly wednesday = new(2026, 6, 3);

        (DateOnly pointerNext, DateOnly weekStartNext) = WeekResolver.ResolveWeek(wednesday, 1);
        (DateOnly pointerPrevious, DateOnly weekStartPrevious) = WeekResolver.ResolveWeek(wednesday, -1);

        Assert.Multiple(() =>
        {
            Assert.That(pointerNext, Is.EqualTo(new DateOnly(2026, 6, 10)));
            Assert.That(weekStartNext, Is.EqualTo(new DateOnly(2026, 6, 8)));
            Assert.That(pointerPrevious, Is.EqualTo(new DateOnly(2026, 5, 27)));
            Assert.That(weekStartPrevious, Is.EqualTo(new DateOnly(2026, 5, 25)));
        });
    }
}
