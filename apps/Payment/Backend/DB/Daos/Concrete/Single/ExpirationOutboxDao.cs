using Backend.DB.Daos.Abstract.Single;
using Backend.Entities;

using DAMA.Software.MySqlOutbox;
using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single;

public sealed class ExpirationOutboxDao : IExpirationOutboxDao
{
    private const string TableName = "expiration_outbox";

    private static readonly OutboxLeaseDescriptor<ExpirationOutboxEvent> LeaseDescriptor = new OutboxLeaseDescriptor<ExpirationOutboxEvent>(
        TableName,
        "Id, AggregateId, EventType, RoutingKey, Payload, OccurredAt, AvailableAt, Attempts",
        "PublishedAt IS NULL",
        MapExpirationOutboxEvent);

    private readonly MySqlConnection _connection;

    public ExpirationOutboxDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task InsertAsync(ExpirationOutboxEvent outboxEvent, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO expiration_outbox " +
                           "(Id, AggregateId, EventType, RoutingKey, Payload, OccurredAt, AvailableAt) " +
                           "VALUES (@Id, @AggregateId, @EventType, @RoutingKey, @Payload, @OccurredAt, @AvailableAt);";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        insertCommand.Parameters.AddWithValue("@Id", outboxEvent.Id.ToString());
        insertCommand.Parameters.AddWithValue("@AggregateId", outboxEvent.AggregateId.ToString());
        insertCommand.Parameters.AddWithValue("@EventType", outboxEvent.EventType);
        insertCommand.Parameters.AddWithValue("@RoutingKey", outboxEvent.RoutingKey);
        insertCommand.Parameters.AddWithValue("@Payload", outboxEvent.Payload);
        insertCommand.Parameters.AddWithValue("@OccurredAt", outboxEvent.OccurredAt);
        insertCommand.Parameters.AddWithValue("@AvailableAt", outboxEvent.AvailableAt);

        await insertCommand.ExecuteNonQueryAsync();
    }

    public Task<List<ExpirationOutboxEvent>> LeasePendingAsync(int batchSize, TimeSpan leaseDuration)
    {
        return MySqlOutboxLeaseHelper.LeaseAsync(_connection, LeaseDescriptor, batchSize, leaseDuration);
    }

    public async Task MarkPublishedAsync(Guid id)
    {
        await MySQLRetryPolicy.EnsureOpenAsync(_connection);
        const string sql = "UPDATE expiration_outbox " +
                           "SET PublishedAt = NOW(6), LeasedUntil = NULL " +
                           "WHERE Id = @id;";
        MySqlCommand updateCommand = new MySqlCommand(sql, _connection);
        updateCommand.Parameters.AddWithValue("@id", id.ToString());
        await updateCommand.ExecuteNonQueryAsync();
    }

    public Task RecordFailureAsync(Guid id, string error)
    {
        return MySqlOutboxLeaseHelper.RecordFailureAsync(_connection, TableName, id, error);
    }

    public async Task<int> DeletePublishedOlderThanAsync(TimeSpan age)
    {
        await MySQLRetryPolicy.EnsureOpenAsync(_connection);
        const string sql = "DELETE FROM expiration_outbox " +
                           "WHERE PublishedAt IS NOT NULL " +
                           "  AND PublishedAt < DATE_SUB(NOW(6), INTERVAL @secs SECOND);";
        MySqlCommand deleteCommand = new MySqlCommand(sql, _connection);
        deleteCommand.Parameters.AddWithValue("@secs", (long)age.TotalSeconds);
        return await deleteCommand.ExecuteNonQueryAsync();
    }

    private static ExpirationOutboxEvent MapExpirationOutboxEvent(MySqlDataReader reader)
    {
        return new ExpirationOutboxEvent
        {
            Id = reader.GetGuid("Id"),
            AggregateId = reader.GetGuid("AggregateId"),
            EventType = reader.GetString("EventType"),
            RoutingKey = reader.GetString("RoutingKey"),
            Payload = reader.GetString("Payload"),
            OccurredAt = reader.GetDateTime("OccurredAt"),
            AvailableAt = reader.GetDateTime("AvailableAt"),
            Attempts = reader.GetInt32("Attempts")
        };
    }
}
