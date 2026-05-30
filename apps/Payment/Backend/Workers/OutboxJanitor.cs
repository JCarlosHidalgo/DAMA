using Backend.DB.Daos.Abstract.Single;

namespace Backend.Workers;

public sealed class OutboxJanitor<TOutboxEvent> : BackgroundService
{
    private static readonly TimeSpan RetentionAge = TimeSpan.FromDays(7);
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxJanitor<TOutboxEvent>> _logger;

    public OutboxJanitor(IServiceProvider serviceProvider, ILogger<OutboxJanitor<TOutboxEvent>> logger)
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
                using var scope = _serviceProvider.CreateScope();
                var outboxDao = scope.ServiceProvider.GetRequiredService<IOutboxDao<TOutboxEvent>>();

                int deleted = await outboxDao.DeletePublishedOlderThanAsync(RetentionAge);
                if (deleted > 0)
                {
                    _logger.LogInformation(
                        "OutboxJanitor<{EventType}> deleted {Count} published rows older than {Age}",
                        typeof(TOutboxEvent).Name,
                        deleted,
                        RetentionAge);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "OutboxJanitor<{EventType}> sweep error", typeof(TOutboxEvent).Name);
            }

            try
            {
                await Task.Delay(SweepInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
