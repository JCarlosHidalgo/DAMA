using Backend.DB.Daos.Abstract.Single.Events;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

namespace Backend.DB.Daos.Concrete.Single.Events;

public sealed class ProcessedEventDao : IProcessedEventDao
{
    private readonly MySqlConnection _connection;

    public ProcessedEventDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<bool> TryMarkProcessedAsync(Guid eventId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO processed_events (EventId, ProcessedAt) " +
                           "VALUES (@id, NOW(6));";
        MySqlCommand command = new MySqlCommand(sql, _connection, sqlTransaction);
        command.Parameters.AddWithValue("@id", eventId.ToString());

        try
        {
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (MySqlException duplicateException) when (duplicateException.Number == 1062)
        {
            return false;
        }
    }
}
