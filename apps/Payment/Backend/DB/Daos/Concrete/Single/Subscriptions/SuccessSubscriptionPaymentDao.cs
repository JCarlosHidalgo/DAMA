using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Entities.Subscriptions;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Subscriptions;

public sealed class SuccessSubscriptionPaymentDao : ISuccessSubscriptionPaymentDao
{
    private readonly MySqlConnection _connection;

    public SuccessSubscriptionPaymentDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<bool> TryCreateAsync(SuccessSubscriptionPayment payment, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO SuccessSubscriptionPayment (Id, TenantId, Level, Cost, Currency, PaidAt) " +
                           "VALUES (@Id, @TenantId, @Level, @Cost, @Currency, @PaidAt);";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        insertCommand.Parameters.AddWithValue("@Id", payment.Id.ToString());
        insertCommand.Parameters.AddWithValue("@TenantId", payment.TenantId.ToString());
        insertCommand.Parameters.AddWithValue("@Level", payment.Level);
        insertCommand.Parameters.AddWithValue("@Cost", payment.Cost);
        insertCommand.Parameters.AddWithValue("@Currency", payment.Currency);
        insertCommand.Parameters.AddWithValue("@PaidAt", payment.PaidAt);

        try
        {
            await insertCommand.ExecuteNonQueryAsync();
            return true;
        }
        catch (MySqlException duplicateException) when (duplicateException.Number == 1062)
        {
            return false;
        }
    }

    public async Task<SuccessSubscriptionPayment?> GetByIdAsync(Guid paymentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT Id, TenantId, Level, Cost, Currency, PaidAt " +
                               "FROM SuccessSubscriptionPayment WHERE Id = @paymentId;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
            selectCommand.Parameters.AddWithValue("@paymentId", paymentId.ToString());

            using MySqlDataReader reader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return (SuccessSubscriptionPayment?)null;
            }

            return new SuccessSubscriptionPayment
            {
                Id = reader.GetGuid("Id"),
                TenantId = reader.GetGuid("TenantId"),
                Level = reader.GetInt32("Level"),
                Cost = reader.GetInt32("Cost"),
                Currency = reader.GetString("Currency"),
                PaidAt = reader.GetDateTime("PaidAt")
            };
        });
    }
}
