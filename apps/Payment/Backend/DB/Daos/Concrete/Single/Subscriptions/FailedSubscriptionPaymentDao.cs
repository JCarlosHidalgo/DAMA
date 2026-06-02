using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Entities.Subscriptions;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

namespace Backend.DB.Daos.Concrete.Single.Subscriptions;

public sealed class FailedSubscriptionPaymentDao : IFailedSubscriptionPaymentDao
{
    private readonly MySqlConnection _connection;

    public FailedSubscriptionPaymentDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<bool> TryCreateAsync(FailedSubscriptionPayment payment, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO FailedSubscriptionPayment (Id, TenantId, Level, Cost, FailedAt) " +
                           "VALUES (@Id, @TenantId, @Level, @Cost, @FailedAt);";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        insertCommand.Parameters.AddWithValue("@Id", payment.Id.ToString());
        insertCommand.Parameters.AddWithValue("@TenantId", payment.TenantId.ToString());
        insertCommand.Parameters.AddWithValue("@Level", payment.Level);
        insertCommand.Parameters.AddWithValue("@Cost", payment.Cost);
        insertCommand.Parameters.AddWithValue("@FailedAt", payment.FailedAt);

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
}
