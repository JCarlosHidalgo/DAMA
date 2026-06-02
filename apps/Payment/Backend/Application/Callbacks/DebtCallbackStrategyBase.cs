using Backend.Entities;
using Backend.Services.Abstract.Todotix;

namespace Backend.Application.Callbacks;

public abstract class DebtCallbackStrategyBase<TPending> : IDebtCallbackStrategy
    where TPending : class
{
    private readonly ITodotixClient _todotixClient;

    protected DebtCallbackStrategyBase(ITodotixClient todotixClient)
    {
        _todotixClient = todotixClient;
    }

    public abstract DebtKind Kind { get; }

    protected abstract Task<TPending?> LoadPendingAsync(Guid transactionId);

    protected abstract Task<string?> ResolveAppKeyAsync(TPending pending);

    protected abstract Task TransitionToSuccessAsync(TPending pending);

    protected abstract Task TransitionToFailedAsync(TPending pending);

    public async Task<bool> TryHandleAsync(Guid transactionId)
    {
        TPending? pending = await LoadPendingAsync(transactionId);
        if (pending is null)
        {
            return false;
        }

        string? appKey = await ResolveAppKeyAsync(pending);
        if (appKey is null)
        {
            return true;
        }

        TodotixDebtState debtState = await _todotixClient.ConsultDebtAsync(transactionId, appKey);
        if (debtState == TodotixDebtState.Paid)
        {
            await TransitionToSuccessAsync(pending);
        }
        else
        {
            await TransitionToFailedAsync(pending);
        }

        return true;
    }
}
