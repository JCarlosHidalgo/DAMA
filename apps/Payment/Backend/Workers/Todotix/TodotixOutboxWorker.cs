using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Entities.Todotix;
using Backend.Logging;
using Backend.Results.Todotix;
using Backend.Services.Abstract.Todotix;

namespace Backend.Workers.Todotix;

public sealed class TodotixOutboxWorker : BackgroundService
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;
    private const int MaxAttempts = 3;
    private const int MaxErrorLength = 500;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TodotixOutboxWorker> _logger;

    public TodotixOutboxWorker(IServiceProvider serviceProvider, ILogger<TodotixOutboxWorker> logger)
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
                await ProcessPendingBatchAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception unexpectedException)
            {
                LogEvents.TodotixOutboxWorkerLoopError(_logger, unexpectedException);
                try
                { await Task.Delay(ErrorBackoff, cancellationToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }

    private async Task ProcessPendingBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var todotixOutboxDao = scope.ServiceProvider.GetRequiredService<ITodotixOutboxDao>();
        var debtPublisher = scope.ServiceProvider.GetRequiredService<IPaymentDebtPublisher>();

        var leasedEvents = await todotixOutboxDao.LeasePendingAsync(BatchSize, LeaseDuration);

        foreach (var outboxEvent in leasedEvents)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await DispatchAsync(todotixOutboxDao, debtPublisher, outboxEvent, cancellationToken);
        }

        if (leasedEvents.Count == 0)
        {
            await Task.Delay(IdleDelay, cancellationToken);
        }
    }

    private async Task DispatchAsync(
        ITodotixOutboxDao todotixOutboxDao,
        IPaymentDebtPublisher debtPublisher,
        TodotixOutboxEvent outboxEvent,
        CancellationToken cancellationToken)
    {
        PublishOutcome outcome = await PublishWithFailureCaptureAsync(debtPublisher, outboxEvent, cancellationToken);
        await ApplyOutcomeAsync(todotixOutboxDao, outboxEvent, outcome);
    }

    private async Task<PublishOutcome> PublishWithFailureCaptureAsync(
        IPaymentDebtPublisher debtPublisher,
        TodotixOutboxEvent outboxEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            return await debtPublisher.PublishAsync(outboxEvent, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception publisherException)
        {
            LogEvents.PaymentOutboxRowFailed(_logger, publisherException, outboxEvent.Id, outboxEvent.Attempts);
            return new PublishOutcome.TransientFailure(publisherException.Message);
        }
    }

    private async Task ApplyOutcomeAsync(
        ITodotixOutboxDao todotixOutboxDao,
        TodotixOutboxEvent outboxEvent,
        PublishOutcome outcome)
    {
        switch (outcome)
        {
            case PublishOutcome.Success:
                await todotixOutboxDao.MarkReadyAsync(outboxEvent.Id);
                break;

            case PublishOutcome.PermanentFailure permanentFailure:
                await todotixOutboxDao.MarkFailedAsync(outboxEvent.Id, Truncate(permanentFailure.Reason));
                break;

            case PublishOutcome.TransientFailure transientFailure:
                if (outboxEvent.Attempts >= MaxAttempts)
                {
                    await todotixOutboxDao.MarkFailedAsync(outboxEvent.Id, Truncate(transientFailure.Reason));
                }
                else
                {
                    await todotixOutboxDao.RecordFailureAsync(outboxEvent.Id, Truncate(transientFailure.Reason));
                }
                break;
        }
    }

    private static string Truncate(string value) => value.Length > MaxErrorLength ? value[..MaxErrorLength] : value;
}
