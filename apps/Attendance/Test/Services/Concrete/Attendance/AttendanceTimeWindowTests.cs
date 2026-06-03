using Backend.Services.Concrete.Attendance;

namespace Test.Services.Concrete.Attendance;

[TestFixture]
public class AttendanceTimeWindowTests
{
    [Test]
    public void TryGetIsNowInside_WithValidTimezone_ReturnsTrue()
    {
        bool succeeded = AttendanceTimeWindow.TryGetIsNowInside(
            "America/La_Paz",
            new TimeOnly(0, 0),
            new TimeOnly(23, 59, 59),
            out bool _);

        Assert.That(succeeded, Is.True);
    }

    [Test]
    public void TryGetIsNowInside_WithUnknownTimezone_ReturnsFalse()
    {
        bool succeeded = AttendanceTimeWindow.TryGetIsNowInside(
            "Continent/NonExistent",
            new TimeOnly(0, 0),
            new TimeOnly(23, 59, 59),
            out bool _);

        Assert.That(succeeded, Is.False);
    }

    [Test]
    public void TryGetIsNowInside_WithEmptyWindow_ReturnsTrueWithIsInsideFalse()
    {
        bool succeeded = AttendanceTimeWindow.TryGetIsNowInside(
            "America/La_Paz",
            new TimeOnly(0, 0),
            new TimeOnly(0, 0),
            out bool isInside);

        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.True);
            Assert.That(isInside, Is.False);
        });
    }

    [Test]
    public void TryGetIsNowInside_WithAllDayWindow_ReturnsTrueWithIsInsideTrue()
    {
        bool succeeded = AttendanceTimeWindow.TryGetIsNowInside(
            "America/La_Paz",
            new TimeOnly(0, 0),
            new TimeOnly(23, 59, 59, 999),
            out bool isInside);

        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.True);
            Assert.That(isInside, Is.True);
        });
    }

    [Test]
    public void TryGetIsNowInside_WithStartAfterCurrentTime_ReturnsIsInsideFalseViaShortCircuit()
    {
        bool succeeded = AttendanceTimeWindow.TryGetIsNowInside(
            "America/La_Paz",
            new TimeOnly(23, 59, 59, 999),
            new TimeOnly(0, 0),
            out bool isInside);

        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.True);
            Assert.That(isInside, Is.False);
        });
    }
}
