using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Dtos.Subscriptions.Input;
using Backend.Entities.Subscriptions;
using Backend.Services.Abstract.Subscriptions;

namespace Backend.Services.Concrete.Subscriptions;

public sealed class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanDao _subscriptionPlanDao;

    public SubscriptionPlanService(ISubscriptionPlanDao subscriptionPlanDao)
    {
        _subscriptionPlanDao = subscriptionPlanDao;
    }

    public async Task UpdateAsync(int level, UpdateSubscriptionPlanDto dto)
    {
        SubscriptionPlan plan = new SubscriptionPlan
        {
            Level = level,
            Price = dto.Price,
            DurationAmount = dto.DurationAmount,
            DurationUnit = dto.DurationUnit
        };

        await _subscriptionPlanDao.UpsertAsync(plan);
    }
}
