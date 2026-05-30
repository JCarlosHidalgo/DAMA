using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single;

public interface IOutboxDao<TOutboxEvent>
{
    Task InsertAsync(TOutboxEvent outboxEvent, ITransactionContext transaction);

    Task<List<TOutboxEvent>> LeasePendingAsync(int batchSize, TimeSpan leaseDuration);

    Task MarkPublishedAsync(Guid id);

    Task RecordFailureAsync(Guid id, string error);

    Task<int> DeletePublishedOlderThanAsync(TimeSpan age);
}
