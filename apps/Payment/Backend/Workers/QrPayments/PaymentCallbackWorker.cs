using Backend.Application.Commands;
using Backend.Application.Mediator;
using Backend.Application.Results;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities.QrPayments;
using Backend.Logging;

namespace Backend.Workers.QrPayments;

public sealed class PaymentCallbackWorker : BackgroundService
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;
    private const int MaxAttempts = 3;
    private const int MaxErrorLength = 500;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentCallbackWorker> _logger;

    public PaymentCallbackWorker(IServiceProvider serviceProvider, ILogger<PaymentCallbackWorker> logger)
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
                LogEvents.PaymentCallbackWorkerLoopError(_logger, unexpectedException);
                try
                { await Task.Delay(ErrorBackoff, cancellationToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }

    private async Task ProcessPendingBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var callbackInboxDao = scope.ServiceProvider.GetRequiredService<IPaymentCallbackInboxDao>();
        var callbackHandler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<ProcessQrCallbackCommand, ProcessQrCallbackResult>>();

        var leasedCallbacks = await callbackInboxDao.LeasePendingAsync(BatchSize, LeaseDuration);

        foreach (var callback in leasedCallbacks)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await DispatchAsync(callbackInboxDao, callbackHandler, callback, cancellationToken);
        }

        if (leasedCallbacks.Count == 0)
        {
            await Task.Delay(IdleDelay, cancellationToken);
        }
    }

    private async Task DispatchAsync(
        IPaymentCallbackInboxDao callbackInboxDao,
        ICommandHandler<ProcessQrCallbackCommand, ProcessQrCallbackResult> callbackHandler,
        PaymentCallback callback,
        CancellationToken cancellationToken)
    {
        try
        {
            await callbackHandler.Handle(
                new ProcessQrCallbackCommand(callback.Id, callback.Error, callback.CancelOrder));
            await callbackInboxDao.MarkProcessedAsync(callback.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception processingException)
        {
            LogEvents.PaymentCallbackFailed(_logger, processingException, callback.Id, callback.Attempts);
            string truncated = Truncate(processingException.Message);
            if (callback.Attempts >= MaxAttempts)
            {
                await callbackInboxDao.MarkFailedAsync(callback.Id, truncated);
            }
            else
            {
                await callbackInboxDao.RecordFailureAsync(callback.Id, truncated);
            }
        }
    }

    private static string Truncate(string value) => value.Length > MaxErrorLength ? value[..MaxErrorLength] : value;
}
