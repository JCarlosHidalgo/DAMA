using Backend.DB.Daos.Abstract.Single;
using Backend.Logging;
using Backend.Services.Abstract;

using DAMA.Software.MySqlOutbox;

namespace Backend.Workers;

public sealed class OutboxRelayWorker<TOutboxEvent> : BackgroundService
    where TOutboxEvent : IOutboxEvent
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(5);
    private const int BatchSize = 100;
    private const int MaxErrorLength = 500;

    private readonly IServiceProvider _serviceProvider;
    private readonly IOutboxPublisher<TOutboxEvent> _publisher;
    private readonly ILogger<OutboxRelayWorker<TOutboxEvent>> _logger;

    public OutboxRelayWorker(IServiceProvider serviceProvider, IOutboxPublisher<TOutboxEvent> publisher, ILogger<OutboxRelayWorker<TOutboxEvent>> logger)
    {
        _serviceProvider = serviceProvider;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBatchAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                LogEvents.OutboxRelayLoopError(_logger, exception, typeof(TOutboxEvent).Name);
                try
                {
                    await Task.Delay(ErrorBackoff, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private async Task ProcessPendingBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxDao = scope.ServiceProvider.GetRequiredService<IOutboxDao<TOutboxEvent>>();

        var batch = await outboxDao.LeasePendingAsync(BatchSize, LeaseDuration);

        if (batch.Count == 0)
        {
            await Task.Delay(IdleDelay, cancellationToken);
            return;
        }

        (TOutboxEvent Event, string? FailureMessage)[] outcomes =
            await Task.WhenAll(batch.Select(outboxEvent => PublishOneAsync(outboxEvent, cancellationToken)));

        foreach ((TOutboxEvent outboxEvent, string? failureMessage) in outcomes)
        {
            if (failureMessage is null)
            {
                await outboxDao.MarkPublishedAsync(outboxEvent.Id);
            }
            else
            {
                await outboxDao.RecordFailureAsync(outboxEvent.Id, failureMessage);
            }
        }
    }

    private async Task<(TOutboxEvent Event, string? FailureMessage)> PublishOneAsync(TOutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        try
        {
            await _publisher.PublishAsync(outboxEvent, cancellationToken);
            return (outboxEvent, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogEvents.OutboxPublishFailed(_logger, exception, outboxEvent.Id);
            return (outboxEvent, exception.Message.Length > MaxErrorLength ? exception.Message[..MaxErrorLength] : exception.Message);
        }
    }
}
