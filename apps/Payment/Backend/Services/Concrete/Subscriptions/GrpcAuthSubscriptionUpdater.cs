using Backend.Services.Abstract.Subscriptions;

using DAMA.Software.GrpcContracts;

namespace Backend.Services.Concrete.Subscriptions;

public sealed class GrpcAuthSubscriptionUpdater : IAuthSubscriptionUpdater
{
    private readonly TenantSubscription.TenantSubscriptionClient _client;

    public GrpcAuthSubscriptionUpdater(TenantSubscription.TenantSubscriptionClient client)
    {
        _client = client;
    }

    public async Task UpdateAsync(Guid tenantId, int level, DateTime newExpiresAtUtc, CancellationToken cancellationToken = default)
    {
        DateTime utc = DateTime.SpecifyKind(newExpiresAtUtc, DateTimeKind.Utc);
        UpdateTenantSubscriptionRequest request = new UpdateTenantSubscriptionRequest
        {
            TenantId = tenantId.ToString("D"),
            Level = level,
            NewExpiresAtUnix = new DateTimeOffset(utc).ToUnixTimeSeconds()
        };

        await _client.UpdateTenantSubscriptionAsync(request, cancellationToken: cancellationToken);
    }
}
