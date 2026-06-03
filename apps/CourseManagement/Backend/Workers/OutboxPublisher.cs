using Backend.DB.Daos.Abstract.Single;
using Backend.Entities;
using Backend.Logging;
using Backend.Messaging;

namespace Backend.Workers;

public class OutboxPublisher : BackgroundService
{
    protected virtual TimeSpan LeaseDuration => TimeSpan.FromSeconds(30);
    protected virtual TimeSpan IdleDelay => TimeSpan.FromSeconds(1);
    protected virtual TimeSpan ErrorBackoff => TimeSpan.FromSeconds(5);
    private const int BatchSize = 100;

    private readonly IServiceProvider _serviceProvider;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(IServiceProvider serviceProvider, IEventPublisher publisher, ILogger<OutboxPublisher> logger)
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
            }
            catch (Exception exception)
            {
                LogEvents.OutboxPublisherLoopError(_logger, exception);
                try
                {
                    await Task.Delay(ErrorBackoff, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }

    private async Task ProcessPendingBatchAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        IOutboxEventDao outboxEventDao = scope.ServiceProvider.GetRequiredService<IOutboxEventDao>();

        List<OutboxEvent> batch = await outboxEventDao.LeasePendingAsync(BatchSize, LeaseDuration);

        if (batch.Count == 0)
        {
            await Task.Delay(IdleDelay, cancellationToken);
            return;
        }

        (OutboxEvent Event, string? FailureMessage)[] outcomes =
            await Task.WhenAll(batch.Select(outboxEvent => PublishOneAsync(outboxEvent, cancellationToken)));

        foreach ((OutboxEvent outboxEvent, string? failureMessage) in outcomes)
        {
            if (failureMessage is null)
            {
                await outboxEventDao.MarkPublishedAsync(outboxEvent.Id);
            }
            else
            {
                await outboxEventDao.RecordFailureAsync(outboxEvent.Id, failureMessage);
            }
        }
    }

    private async Task<(OutboxEvent Event, string? FailureMessage)> PublishOneAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
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
            return (outboxEvent, exception.Message.Length > 500 ? exception.Message[..500] : exception.Message);
        }
    }
}
