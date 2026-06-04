using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Dtos.Subscriptions.Input;
using Backend.Entities.Subscriptions;
using Backend.Options;
using Backend.Services.Abstract.Subscriptions;

using Microsoft.Extensions.Options;

namespace Backend.Services.Concrete.Subscriptions;

public sealed class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanDao _subscriptionPlanDao;
    private readonly IOptions<CurrencyOptions> _currencyOptions;

    public SubscriptionPlanService(ISubscriptionPlanDao subscriptionPlanDao,
                                   IOptions<CurrencyOptions> currencyOptions)
    {
        _subscriptionPlanDao = subscriptionPlanDao;
        _currencyOptions = currencyOptions;
    }

    public async Task UpdateAsync(int level, UpdateSubscriptionPlanDto dto)
    {
        SubscriptionPlan plan = new SubscriptionPlan
        {
            Level = level,
            Price = dto.Price,
            Currency = _currencyOptions.Value.Default,
            DurationAmount = dto.DurationAmount,
            DurationUnit = dto.DurationUnit
        };

        await _subscriptionPlanDao.UpsertAsync(plan);
    }
}
