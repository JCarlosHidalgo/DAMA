namespace Backend.DB.Daos.Abstract.Single.Subscriptions;

public readonly record struct SubscriptionRevenueTotalRow(int TotalRevenue, int PaymentCount);

public readonly record struct SubscriptionRevenueMonthRow(int Year, int Month, int Revenue, int Count);

public readonly record struct SubscriptionRevenueTierRow(int Level, int Revenue, int Count);

public interface IAdminSubscriptionAnalyticsDao
{
    Task<SubscriptionRevenueTotalRow> GetRevenueTotalAsync();

    Task<List<SubscriptionRevenueMonthRow>> GetRevenueByMonthAsync(DateTime fromDate, DateTime toDate);

    Task<List<SubscriptionRevenueTierRow>> GetRevenueByTierAsync();
}
