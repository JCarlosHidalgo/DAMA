using Backend.Entities;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single;

public interface IOutboxEventDao
{
    Task InsertAsync(OutboxEvent outboxEvent, ITransactionContext transaction);
    Task<List<OutboxEvent>> LeasePendingAsync(int batchSize, TimeSpan leaseDuration);
    Task MarkPublishedAsync(Guid id);
    Task RecordFailureAsync(Guid id, string error);
    Task<int> DeletePublishedOlderThanAsync(TimeSpan age);
}
