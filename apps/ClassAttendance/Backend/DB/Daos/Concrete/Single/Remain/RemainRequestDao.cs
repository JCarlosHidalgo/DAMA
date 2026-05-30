using Backend.DB.Daos.Abstract.Single.Remain;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

namespace Backend.DB.Daos.Concrete.Single.Remain;

public sealed class RemainRequestDao : IRemainRequestDao
{
    private readonly MySqlConnection _connection;

    public RemainRequestDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<bool> TryMarkProcessedAsync(Guid requestId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO processed_remain_requests (RequestId, ProcessedAt) " +
                           "VALUES (@id, NOW(6));";
        MySqlCommand command = new MySqlCommand(sql, _connection, sqlTransaction);
        command.Parameters.AddWithValue("@id", requestId.ToString());

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
