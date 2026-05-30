using Backend.DB.Daos.Abstract.Single;
using Backend.Entities;

using DAMA.Software.MySqlOutbox;
using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single;

public sealed class OutboxEventDao : IOutboxEventDao
{
    private const string TableName = "outbox_events";

    private static readonly OutboxLeaseDescriptor<OutboxEvent> LeaseDescriptor = new OutboxLeaseDescriptor<OutboxEvent>(
        TableName,
        "Id, AggregateType, AggregateId, EventType, RoutingKey, Payload, OccurredAt, Attempts",
        "PublishedAt IS NULL",
        MapOutboxEvent);

    private readonly MySqlConnection _connection;

    public OutboxEventDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task InsertAsync(OutboxEvent outboxEvent, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO outbox_events " +
                           "(Id, AggregateType, AggregateId, EventType, RoutingKey, Payload, OccurredAt) " +
                           "VALUES (@Id, @AggregateType, @AggregateId, @EventType, @RoutingKey, @Payload, @OccurredAt);";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        insertCommand.Parameters.AddWithValue("@Id", outboxEvent.Id.ToString());
        insertCommand.Parameters.AddWithValue("@AggregateType", outboxEvent.AggregateType);
        insertCommand.Parameters.AddWithValue("@AggregateId", outboxEvent.AggregateId.ToString());
        insertCommand.Parameters.AddWithValue("@EventType", outboxEvent.EventType);
        insertCommand.Parameters.AddWithValue("@RoutingKey", outboxEvent.RoutingKey);
        insertCommand.Parameters.AddWithValue("@Payload", outboxEvent.Payload);
        insertCommand.Parameters.AddWithValue("@OccurredAt", outboxEvent.OccurredAt);

        await insertCommand.ExecuteNonQueryAsync();
    }

    public Task<List<OutboxEvent>> LeasePendingAsync(int batchSize, TimeSpan leaseDuration)
    {
        return MySqlOutboxLeaseHelper.LeaseAsync(_connection, LeaseDescriptor, batchSize, leaseDuration);
    }

    public async Task MarkPublishedAsync(Guid id)
    {
        const string sql = "UPDATE outbox_events " +
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
        const string sql = "DELETE FROM outbox_events " +
                           "WHERE PublishedAt IS NOT NULL " +
                           "  AND PublishedAt < DATE_SUB(NOW(6), INTERVAL @secs SECOND);";
        MySqlCommand deleteCommand = new MySqlCommand(sql, _connection);
        deleteCommand.Parameters.AddWithValue("@secs", (long)age.TotalSeconds);
        return await deleteCommand.ExecuteNonQueryAsync();
    }

    private static OutboxEvent MapOutboxEvent(MySqlDataReader reader)
    {
        return new OutboxEvent
        {
            Id = reader.GetGuid("Id"),
            AggregateType = reader.GetString("AggregateType"),
            AggregateId = reader.GetGuid("AggregateId"),
            EventType = reader.GetString("EventType"),
            RoutingKey = reader.GetString("RoutingKey"),
            Payload = reader.GetString("Payload"),
            OccurredAt = reader.GetDateTime("OccurredAt"),
            Attempts = reader.GetInt32("Attempts")
        };
    }
}
