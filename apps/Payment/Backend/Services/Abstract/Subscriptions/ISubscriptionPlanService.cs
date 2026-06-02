using Backend.Dtos.Subscriptions.Input;

namespace Backend.Services.Abstract.Subscriptions;

public interface ISubscriptionPlanService
{
    Task UpdateAsync(int level, UpdateSubscriptionPlanDto dto);
}
