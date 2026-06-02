using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Entities.Subscriptions;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Subscriptions;

public sealed class PendingSubscriptionPaymentDao : IPendingSubscriptionPaymentDao
{
    private const string SelectColumns =
        "Id, TenantId, Level, Cost, QrImageUrl, CreatedAt, ExpiresAt";

    private readonly MySqlConnection _connection;

    public PendingSubscriptionPaymentDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task CreateAsync(PendingSubscriptionPayment payment, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO PendingSubscriptionPayment (Id, TenantId, Level, Cost, QrImageUrl, ExpiresAt) " +
                           "VALUES (@Id, @TenantId, @Level, @Cost, @QrImageUrl, @ExpiresAt);";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        insertCommand.Parameters.AddWithValue("@Id", payment.Id.ToString());
        insertCommand.Parameters.AddWithValue("@TenantId", payment.TenantId.ToString());
        insertCommand.Parameters.AddWithValue("@Level", payment.Level);
        insertCommand.Parameters.AddWithValue("@Cost", payment.Cost);
        insertCommand.Parameters.AddWithValue("@QrImageUrl", (object?)payment.QrImageUrl ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@ExpiresAt", payment.ExpiresAt);

        await insertCommand.ExecuteNonQueryAsync();
    }

    public async Task<PendingSubscriptionPayment?> GetByIdAsync(Guid paymentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            string sql = $"SELECT {SelectColumns} FROM PendingSubscriptionPayment WHERE Id = @paymentId;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
            selectCommand.Parameters.AddWithValue("@paymentId", paymentId.ToString());

            using MySqlDataReader reader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapReader(reader) : null;
        });
    }

    public async Task<PendingSubscriptionPayment?> GetByIdForTenantAsync(Guid tenantId, Guid paymentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            string sql = $"SELECT {SelectColumns} FROM PendingSubscriptionPayment " +
                         "WHERE Id = @paymentId AND TenantId = @tenantId;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
            selectCommand.Parameters.AddWithValue("@paymentId", paymentId.ToString());
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());

            using MySqlDataReader reader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapReader(reader) : null;
        });
    }

    public async Task<int> CountActiveForTenantAsync(Guid tenantId, DateTime nowUtc)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT COUNT(*) FROM PendingSubscriptionPayment " +
                               "WHERE TenantId = @tenantId AND ExpiresAt > @nowUtc;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters.AddWithValue("@nowUtc", nowUtc);

            object? countResult = await selectCommand.ExecuteScalarAsync();
            return Convert.ToInt32(countResult ?? 0);
        });
    }

    public async Task UpdateQrImageUrlAsync(Guid paymentId, string qrImageUrl)
    {
        await MySQLRetryPolicy.EnsureOpenAsync(_connection);
        const string sql = "UPDATE PendingSubscriptionPayment SET QrImageUrl = @qrImageUrl WHERE Id = @paymentId;";
        MySqlCommand updateCommand = new MySqlCommand(sql, _connection);
        updateCommand.Parameters.AddWithValue("@qrImageUrl", qrImageUrl);
        updateCommand.Parameters.AddWithValue("@paymentId", paymentId.ToString());
        await updateCommand.ExecuteNonQueryAsync();
    }

    public async Task<bool> DeleteAsync(Guid paymentId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "DELETE FROM PendingSubscriptionPayment WHERE Id = @paymentId;";
        MySqlCommand deleteCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        deleteCommand.Parameters.AddWithValue("@paymentId", paymentId.ToString());
        int affectedRowCount = await deleteCommand.ExecuteNonQueryAsync();
        return affectedRowCount > 0;
    }

    private static PendingSubscriptionPayment MapReader(MySqlDataReader reader)
    {
        int qrImageUrlOrdinal = reader.GetOrdinal("QrImageUrl");
        return new PendingSubscriptionPayment
        {
            Id = reader.GetGuid("Id"),
            TenantId = reader.GetGuid("TenantId"),
            Level = reader.GetInt32("Level"),
            Cost = reader.GetInt32("Cost"),
            QrImageUrl = reader.IsDBNull(qrImageUrlOrdinal) ? null : reader.GetString(qrImageUrlOrdinal),
            CreatedAt = reader.GetDateTime("CreatedAt"),
            ExpiresAt = reader.GetDateTime("ExpiresAt")
        };
    }
}
