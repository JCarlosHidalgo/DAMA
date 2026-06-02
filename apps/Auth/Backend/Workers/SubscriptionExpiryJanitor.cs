using Backend.DB.Daos.Abstract.Single.Tenants;

namespace Backend.Workers;

public class SubscriptionExpiryJanitor : BackgroundService
{
    protected virtual TimeSpan SweepInterval => TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionExpiryJanitor> _logger;

    public SubscriptionExpiryJanitor(IServiceProvider serviceProvider, ILogger<SubscriptionExpiryJanitor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                ITenantAllowedServicesDao dao =
                    scope.ServiceProvider.GetRequiredService<ITenantAllowedServicesDao>();

                int reset = await dao.ResetExpiredAsync(DateTime.UtcNow);
                if (reset > 0)
                {
                    _logger.LogInformation(
                        "SubscriptionExpiryJanitor reset {Count} expired tenant subscriptions to index 0", reset);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception sweepException)
            {
                _logger.LogError(sweepException, "SubscriptionExpiryJanitor sweep error");
            }

            try
            {
                await Task.Delay(SweepInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
