using Backend.DB.Daos.Abstract.Single;
using Backend.Logging;

namespace Backend.Workers;

public sealed class ProcessedEventsJanitor : BackgroundService
{
    private static readonly TimeSpan RetentionAge = TimeSpan.FromDays(7);
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessedEventsJanitor> _logger;

    public ProcessedEventsJanitor(IServiceProvider serviceProvider, ILogger<ProcessedEventsJanitor> logger)
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
                var processedEventDao = scope.ServiceProvider.GetRequiredService<IProcessedEventDao>();

                int deleted = await processedEventDao.DeleteOlderThanAsync(RetentionAge);
                if (deleted > 0)
                {
                    LogEvents.ProcessedEventsJanitorDeleted(_logger, deleted, RetentionAge);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                LogEvents.ProcessedEventsJanitorSweepError(_logger, exception);
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
