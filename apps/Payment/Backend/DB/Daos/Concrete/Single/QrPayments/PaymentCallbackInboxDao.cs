using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities.QrPayments;

using DAMA.Software.MySqlOutbox;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.QrPayments;

public sealed class PaymentCallbackInboxDao : IPaymentCallbackInboxDao
{
    private const string TableName = "payment_callback_inbox";

    private static readonly OutboxLeaseDescriptor<PaymentCallback> LeaseDescriptor = new OutboxLeaseDescriptor<PaymentCallback>(
        TableName,
        "Id, Error, CancelOrder, OccurredAt, Attempts, LastError, Status",
        "ProcessedAt IS NULL AND Status = 'Pending'",
        MapPaymentCallback);

    private readonly MySqlConnection _connection;

    public PaymentCallbackInboxDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<bool> TryEnqueueAsync(Guid transactionId, int error, int cancelOrder)
    {
        await MySQLRetryPolicy.EnsureOpenAsync(_connection);
        const string sql = "INSERT INTO payment_callback_inbox " +
                           "(Id, Error, CancelOrder, OccurredAt, Status) " +
                           "VALUES (@Id, @Error, @CancelOrder, NOW(6), 'Pending');";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection);
        insertCommand.Parameters.AddWithValue("@Id", transactionId.ToString());
        insertCommand.Parameters.AddWithValue("@Error", error);
        insertCommand.Parameters.AddWithValue("@CancelOrder", cancelOrder);

        try
        {
            await insertCommand.ExecuteNonQueryAsync();
            return true;
        }
        catch (MySqlException duplicateKeyException) when (duplicateKeyException.Number == 1062)
        {
            return false;
        }
    }

    public Task<List<PaymentCallback>> LeasePendingAsync(int batchSize, TimeSpan leaseDuration)
    {
        return MySqlOutboxLeaseHelper.LeaseAsync(_connection, LeaseDescriptor, batchSize, leaseDuration);
    }

    public async Task MarkProcessedAsync(Guid id)
    {
        await MySQLRetryPolicy.EnsureOpenAsync(_connection);
        const string sql = "UPDATE payment_callback_inbox " +
                           "SET ProcessedAt = NOW(6), LeasedUntil = NULL, Status = 'Processed' " +
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
        const string sql = "UPDATE payment_callback_inbox " +
                           "SET ProcessedAt = NOW(6), LeasedUntil = NULL, Status = 'Failed', LastError = @error " +
                           "WHERE Id = @id;";
        MySqlCommand updateCommand = new MySqlCommand(sql, _connection);
        updateCommand.Parameters.AddWithValue("@id", id.ToString());
        updateCommand.Parameters.AddWithValue("@error", error);
        await updateCommand.ExecuteNonQueryAsync();
    }

    private static PaymentCallback MapPaymentCallback(MySqlDataReader reader)
    {
        int lastErrorOrdinal = reader.GetOrdinal("LastError");
        return new PaymentCallback
        {
            Id = reader.GetGuid("Id"),
            Error = reader.GetInt32("Error"),
            CancelOrder = reader.GetInt32("CancelOrder"),
            OccurredAt = reader.GetDateTime("OccurredAt"),
            Attempts = reader.GetInt32("Attempts"),
            LastError = reader.IsDBNull(lastErrorOrdinal) ? null : reader.GetString(lastErrorOrdinal),
            Status = reader.GetString("Status")
        };
    }
}
