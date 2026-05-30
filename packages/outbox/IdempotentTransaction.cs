using DAMA.Software.MySqlUnitOfWork;

namespace DAMA.Software.MySqlOutbox;

public static class IdempotentTransaction
{
    public static async Task<TOutcome> RunAsync<TOutcome>(
        IUnitOfWork unitOfWork,
        IProcessedEventStore processedEvents,
        Guid eventId,
        TOutcome alreadyProcessedOutcome,
        Func<ITransactionScope, Task<TOutcome>> applyIfFirstTime)
    {
        await using ITransactionScope scope = await unitOfWork.BeginAsync();
        bool isFirstTime = await processedEvents.TryMarkProcessedAsync(eventId, scope);
        if (!isFirstTime)
        {
            await scope.CommitAsync();
            return alreadyProcessedOutcome;
        }
        TOutcome outcome = await applyIfFirstTime(scope);
        await scope.CommitAsync();
        return outcome;
    }
}
