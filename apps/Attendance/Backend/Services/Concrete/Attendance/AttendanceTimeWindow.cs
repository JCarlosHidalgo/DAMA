namespace Backend.Services.Concrete.Attendance;

public static class AttendanceTimeWindow
{
    public static bool TryGetIsNowInside(string ianaTimezoneId, TimeOnly windowStart, TimeOnly windowEnd, out bool isInside)
    {
        isInside = false;
        TimeZoneInfo timezoneInfo;
        try
        {
            timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(ianaTimezoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }

        DateTime nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezoneInfo);
        TimeOnly nowAsTimeOnly = TimeOnly.FromDateTime(nowLocal);
        isInside = nowAsTimeOnly >= windowStart && nowAsTimeOnly < windowEnd;
        return true;
    }
}
