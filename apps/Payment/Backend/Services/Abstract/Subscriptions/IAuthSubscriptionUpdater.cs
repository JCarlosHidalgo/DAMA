namespace Backend.Services.Abstract.Subscriptions;

public interface IAuthSubscriptionUpdater
{
    Task UpdateAsync(Guid tenantId, int level, DateTime newExpiresAtUtc, CancellationToken cancellationToken = default);
}
