namespace Backend.Application.Schedules;

public static class WeekResolver
{
    public static DateOnly TenantToday(string ianaTimezoneId, DateTime utcNow)
    {
        try
        {
            TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(ianaTimezoneId);
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone));
        }
        catch (Exception timezoneException) when (timezoneException is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return DateOnly.FromDateTime(utcNow);
        }
    }

    public static (DateOnly Pointer, DateOnly WeekStart) ResolveWeek(DateOnly today, int weekPaginationIndex)
    {
        DateOnly pointer = today.AddDays(weekPaginationIndex * 7);
        int mondayOffset = ((int)pointer.DayOfWeek + 6) % 7;
        DateOnly weekStart = pointer.AddDays(-mondayOffset);
        return (pointer, weekStart);
    }
}
