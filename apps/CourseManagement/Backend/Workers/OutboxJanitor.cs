using Backend.DB.Daos.Abstract.Single;
using Backend.Logging;

namespace Backend.Workers;

public class OutboxJanitor : BackgroundService
{
    protected virtual TimeSpan RetentionAge => TimeSpan.FromDays(7);
    protected virtual TimeSpan SweepInterval => TimeSpan.FromHours(24);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxJanitor> _logger;

    public OutboxJanitor(IServiceProvider serviceProvider, ILogger<OutboxJanitor> logger)
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
                IOutboxEventDao outboxEventDao = scope.ServiceProvider.GetRequiredService<IOutboxEventDao>();

                int deleted = await outboxEventDao.DeletePublishedOlderThanAsync(RetentionAge);
                if (deleted > 0)
                {
                    LogEvents.OutboxJanitorDeletedPublished(_logger, deleted, RetentionAge);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception sweepException)
            {
                LogEvents.OutboxJanitorSweepError(_logger, sweepException);
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
