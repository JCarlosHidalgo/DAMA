using Backend.Entities;

namespace Backend.Application.Callbacks;

public interface IDebtCallbackStrategy
{
    DebtKind Kind { get; }

    Task<bool> TryHandleAsync(Guid transactionId);
}
