using Backend.Dtos.Admin.Output;

namespace Backend.Services.Abstract.Admin;

public interface IAdminAnalyticsService
{
    Task<SubscriptionRevenueTotalDto> GetRevenueTotalAsync();

    Task<List<SubscriptionRevenuePointDto>> GetRevenueTimelineAsync(DateTime fromDate, DateTime toDate);

    Task<List<SubscriptionRevenueByTierDto>> GetRevenueByTierAsync();
}
