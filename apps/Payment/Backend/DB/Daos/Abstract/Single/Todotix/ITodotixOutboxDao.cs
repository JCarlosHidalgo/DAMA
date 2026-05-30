using Backend.Entities.Todotix;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single.Todotix;

public interface ITodotixOutboxDao
{
    Task InsertAsync(TodotixOutboxEvent outboxEvent, ITransactionContext transaction);

    Task<List<TodotixOutboxEvent>> LeasePendingAsync(int batchSize, TimeSpan leaseDuration);

    Task MarkReadyAsync(Guid id);

    Task RecordFailureAsync(Guid id, string error);

    Task MarkFailedAsync(Guid id, string error);

    Task<TodotixOutboxEvent?> GetByPendingIdAsync(Guid pendingId);
}
