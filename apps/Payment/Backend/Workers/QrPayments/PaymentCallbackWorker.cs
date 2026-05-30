using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities.QrPayments;
using Backend.Services.Abstract.QrPayments;

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
                using var scope = _serviceProvider.CreateScope();
                var callbackInboxDao = scope.ServiceProvider.GetRequiredService<IPaymentCallbackInboxDao>();
                var callbackService = scope.ServiceProvider.GetRequiredService<IQrCallbackService>();

                var leasedCallbacks = await callbackInboxDao.LeasePendingAsync(BatchSize, LeaseDuration);

                foreach (var callback in leasedCallbacks)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await DispatchAsync(callbackInboxDao, callbackService, callback, cancellationToken);
                }

                if (leasedCallbacks.Count == 0)
                {
                    await Task.Delay(IdleDelay, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception unexpectedException)
            {
                _logger.LogError(unexpectedException, "PaymentCallbackWorker loop error");
                try
                { await Task.Delay(ErrorBackoff, cancellationToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }

    private async Task DispatchAsync(
        IPaymentCallbackInboxDao callbackInboxDao,
        IQrCallbackService callbackService,
        PaymentCallback callback,
        CancellationToken cancellationToken)
    {
        try
        {
            await callbackService.HandleCallbackAsync(callback.Id, callback.Error, callback.CancelOrder);
            await callbackInboxDao.MarkProcessedAsync(callback.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception processingException)
        {
            _logger.LogWarning(processingException, "Payment callback {Id} failed (attempt {Attempts})", callback.Id, callback.Attempts);
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
