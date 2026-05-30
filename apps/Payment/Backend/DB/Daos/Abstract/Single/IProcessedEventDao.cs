using DAMA.Software.MySqlOutbox;

namespace Backend.DB.Daos.Abstract.Single;

public interface IProcessedEventDao : IProcessedEventStore
{
    Task<int> DeleteOlderThanAsync(TimeSpan age);
}
