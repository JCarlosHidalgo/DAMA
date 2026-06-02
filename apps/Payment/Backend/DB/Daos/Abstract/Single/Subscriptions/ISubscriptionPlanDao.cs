using Backend.Entities.Subscriptions;

namespace Backend.DB.Daos.Abstract.Single.Subscriptions;

public interface ISubscriptionPlanDao
{
    Task<SubscriptionPlan?> GetByLevelAsync(int level);

    Task<List<SubscriptionPlan>> GetAllAsync();

    Task UpsertAsync(SubscriptionPlan plan);
}
