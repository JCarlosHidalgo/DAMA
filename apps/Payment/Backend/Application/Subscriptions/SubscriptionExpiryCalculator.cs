using Backend.Entities.Subscriptions;

namespace Backend.Application.Subscriptions;

public static class SubscriptionExpiryCalculator
{
    public static DateTime ComputeExpiryUtc(DateTime fromUtc, int durationAmount, string durationUnit)
    {
        if (!Enum.TryParse(durationUnit, ignoreCase: true, out SubscriptionDurationUnit unit))
        {
            throw new ArgumentOutOfRangeException(nameof(durationUnit), durationUnit, "Unknown subscription duration unit.");
        }

        return unit switch
        {
            SubscriptionDurationUnit.Day => fromUtc.AddDays(durationAmount),
            SubscriptionDurationUnit.Week => fromUtc.AddDays(7 * durationAmount),
            SubscriptionDurationUnit.Month => fromUtc.AddMonths(durationAmount),
            _ => throw new ArgumentOutOfRangeException(nameof(durationUnit), durationUnit, "Unknown subscription duration unit.")
        };
    }
}
