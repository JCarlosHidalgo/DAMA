using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Entities.Todotix;

using DAMA.Software.MySqlOutbox;
using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Todotix;

public sealed class TodotixOutboxDao : ITodotixOutboxDao
{
    private const string TableName = "todotix_outbox";

    private static readonly OutboxLeaseDescriptor<TodotixOutboxEvent> LeaseDescriptor = new OutboxLeaseDescriptor<TodotixOutboxEvent>(
        TableName,
        "Id, PendingId, TenantId, PayloadJson, OccurredAt, Attempts, LastError, Status",
        "ProcessedAt IS NULL AND Status = 'Pending'",
        MapTodotixOutboxEvent);

    private readonly MySqlConnection _connection;

    public TodotixOutboxDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task InsertAsync(TodotixOutboxEvent outboxEvent, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO todotix_outbox " +
                           "(Id, PendingId, TenantId, PayloadJson, OccurredAt, Status) " +
                           "VALUES (@Id, @PendingId, @TenantId, @PayloadJson, @OccurredAt, 'Pending');";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        insertCommand.Parameters.AddWithValue("@Id", outboxEvent.Id.ToString());
        insertCommand.Parameters.AddWithValue("@PendingId", outboxEvent.PendingId.ToString());
        insertCommand.Parameters.AddWithValue("@TenantId", outboxEvent.TenantId.ToString());
        insertCommand.Parameters.AddWithValue("@PayloadJson", outboxEvent.PayloadJson);
        insertCommand.Parameters.AddWithValue("@OccurredAt", outboxEvent.OccurredAt);

        await insertCommand.ExecuteNonQueryAsync();
    }

    public Task<List<TodotixOutboxEvent>> LeasePendingAsync(int batchSize, TimeSpan leaseDuration)
    {
        return MySqlOutboxLeaseHelper.LeaseAsync(_connection, LeaseDescriptor, batchSize, leaseDuration);
    }

    public async Task MarkReadyAsync(Guid id)
    {
        await MySQLRetryPolicy.EnsureOpenAsync(_connection);
        const string sql = "UPDATE todotix_outbox " +
                           "SET ProcessedAt = NOW(6), LeasedUntil = NULL, Status = 'Ready' " +
                           "WHERE Id = @id;";
        MySqlCommand updateCommand = new MySqlCommand(sql, _connection);
        updateCommand.Parameters.AddWithValue("@id", id.ToString());
        await updateCommand.ExecuteNonQueryAsync();
    }

    public Task RecordFailureAsync(Guid id, string error)
    {
        return MySqlOutboxLeaseHelper.RecordFailureAsync(_connection, TableName, id, error);
    }

    public async Task MarkFailedAsync(Guid id, string error)
    {
        await MySQLRetryPolicy.EnsureOpenAsync(_connection);
        const string sql = "UPDATE todotix_outbox " +
                           "SET ProcessedAt = NOW(6), LeasedUntil = NULL, Status = 'Failed', LastError = @error " +
                           "WHERE Id = @id;";
        MySqlCommand updateCommand = new MySqlCommand(sql, _connection);
        updateCommand.Parameters.AddWithValue("@id", id.ToString());
        updateCommand.Parameters.AddWithValue("@error", error);
        await updateCommand.ExecuteNonQueryAsync();
    }

    public async Task<TodotixOutboxEvent?> GetByPendingIdAsync(Guid pendingId)
    {
        await MySQLRetryPolicy.EnsureOpenAsync(_connection);
        const string sql = "SELECT Id, PendingId, TenantId, PayloadJson, OccurredAt, Attempts, LastError, Status " +
                           "FROM todotix_outbox WHERE PendingId = @pendingId LIMIT 1;";
        MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
        selectCommand.Parameters.AddWithValue("@pendingId", pendingId.ToString());

        using var reader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapTodotixOutboxEvent(reader);
    }

    private static TodotixOutboxEvent MapTodotixOutboxEvent(MySqlDataReader reader)
    {
        int lastErrorOrdinal = reader.GetOrdinal("LastError");
        return new TodotixOutboxEvent
        {
            Id = reader.GetGuid("Id"),
            PendingId = reader.GetGuid("PendingId"),
            TenantId = reader.GetGuid("TenantId"),
            PayloadJson = reader.GetString("PayloadJson"),
            OccurredAt = reader.GetDateTime("OccurredAt"),
            Attempts = reader.GetInt32("Attempts"),
            LastError = reader.IsDBNull(lastErrorOrdinal) ? null : reader.GetString(lastErrorOrdinal),
            Status = reader.GetString("Status")
        };
    }
}
