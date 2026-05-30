using DAMA.Software.MySqlUnitOfWork;

namespace DAMA.Software.MySqlOutbox;

public interface IProcessedEventStore
{
    Task<bool> TryMarkProcessedAsync(Guid eventId, ITransactionContext transaction);
}
