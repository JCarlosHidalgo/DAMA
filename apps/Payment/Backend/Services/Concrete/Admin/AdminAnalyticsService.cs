using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Dtos.Admin.Output;
using Backend.Options;
using Backend.Services.Abstract.Admin;

using Microsoft.Extensions.Options;

namespace Backend.Services.Concrete.Admin;

public class AdminAnalyticsService : IAdminAnalyticsService
{
    private readonly IAdminSubscriptionAnalyticsDao _analyticsDao;
    private readonly IOptions<CurrencyOptions> _currencyOptions;

    public AdminAnalyticsService(IAdminSubscriptionAnalyticsDao analyticsDao,
                                 IOptions<CurrencyOptions> currencyOptions)
    {
        _analyticsDao = analyticsDao;
        _currencyOptions = currencyOptions;
    }

    public async Task<SubscriptionRevenueTotalDto> GetRevenueTotalAsync()
    {
        SubscriptionRevenueTotalRow total = await _analyticsDao.GetRevenueTotalAsync();

        return new SubscriptionRevenueTotalDto
        {
            TotalRevenue = total.TotalRevenue,
            PaymentCount = total.PaymentCount,
            Currency = _currencyOptions.Value.Default
        };
    }

    public async Task<List<SubscriptionRevenuePointDto>> GetRevenueTimelineAsync(DateTime fromDate, DateTime toDate)
    {
        List<SubscriptionRevenueMonthRow> rows = await _analyticsDao.GetRevenueByMonthAsync(fromDate, toDate);

        return rows
            .Select(row => new SubscriptionRevenuePointDto
            {
                Year = row.Year,
                Month = row.Month,
                Amount = row.Revenue,
                Count = row.Count
            })
            .ToList();
    }

    public async Task<List<SubscriptionRevenueByTierDto>> GetRevenueByTierAsync()
    {
        List<SubscriptionRevenueTierRow> rows = await _analyticsDao.GetRevenueByTierAsync();

        return rows
            .Select(row => new SubscriptionRevenueByTierDto
            {
                Level = row.Level,
                Revenue = row.Revenue,
                Count = row.Count
            })
            .ToList();
    }
}
