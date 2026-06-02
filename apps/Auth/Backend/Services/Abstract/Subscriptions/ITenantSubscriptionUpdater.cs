namespace Backend.Services.Abstract.Subscriptions;

public interface ITenantSubscriptionUpdater
{
    Task UpdateAsync(Guid tenantId, int level, DateTime newExpiresAtUtc);
}
