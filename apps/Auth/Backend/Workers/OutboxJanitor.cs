using Backend.DB.Daos.Abstract.Single;

namespace Backend.Workers;

public class OutboxJanitor : BackgroundService
{
    protected virtual TimeSpan RetentionAge => TimeSpan.FromDays(7);
    protected virtual TimeSpan SweepInterval => TimeSpan.FromHours(24);

    private readonly IServiceProvider _sp;
    private readonly ILogger<OutboxJanitor> _log;

    public OutboxJanitor(IServiceProvider sp, ILogger<OutboxJanitor> log)
    {
        _sp = sp;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var dao = scope.ServiceProvider.GetRequiredService<IOutboxEventDao>();

                int deleted = await dao.DeletePublishedOlderThanAsync(RetentionAge);
                if (deleted > 0)
                {
                    _log.LogInformation("OutboxJanitor deleted {Count} published events older than {Age}", deleted, RetentionAge);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "OutboxJanitor sweep error");
            }

            try
            { await Task.Delay(SweepInterval, ct); }
            catch (OperationCanceledException) { }
        }
    }
}
