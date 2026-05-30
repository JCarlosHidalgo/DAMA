using Backend.Events;
using Backend.Results.QrPayments;

namespace Backend.Services.Abstract.Events;

public interface IDebtExpiredHandler
{
    Task<HandleDebtExpiredOutcome> HandleAsync(DebtExpiredEvent debtExpiredEvent, CancellationToken cancellationToken);
}
