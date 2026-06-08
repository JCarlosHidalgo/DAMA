using System.Data;

using Backend.DB.Daos.Abstract.Single.Subscriptions;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Subscriptions;

public sealed class AdminSubscriptionAnalyticsDao : IAdminSubscriptionAnalyticsDao
{
    private readonly MySqlConnection _connection;

    public AdminSubscriptionAnalyticsDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<SubscriptionRevenueTotalRow> GetRevenueTotalAsync()
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = new MySqlCommand("GetSubscriptionRevenueTotal", _connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using MySqlDataReader reader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new SubscriptionRevenueTotalRow(0, 0);
            }

            return new SubscriptionRevenueTotalRow(
                Convert.ToInt32(reader["TotalRevenue"]),
                reader.GetInt32("PaymentCount"));
        });
    }

    public async Task<List<SubscriptionRevenueMonthRow>> GetRevenueByMonthAsync(DateTime fromDate, DateTime toDate)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = new MySqlCommand("GetSubscriptionRevenueByMonth", _connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            selectCommand.Parameters.AddWithValue("@fromDate", fromDate);
            selectCommand.Parameters["@fromDate"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@toDate", toDate);
            selectCommand.Parameters["@toDate"].Direction = ParameterDirection.Input;

            using MySqlDataReader reader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            List<SubscriptionRevenueMonthRow> rows = new List<SubscriptionRevenueMonthRow>();
            while (await reader.ReadAsync())
            {
                rows.Add(new SubscriptionRevenueMonthRow(
                    reader.GetInt32("Yr"),
                    reader.GetInt32("Mo"),
                    Convert.ToInt32(reader["Revenue"]),
                    reader.GetInt32("Cnt")));
            }
            return rows;
        });
    }

    public async Task<List<SubscriptionRevenueTierRow>> GetRevenueByTierAsync()
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = new MySqlCommand("GetSubscriptionRevenueByTier", _connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using MySqlDataReader reader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            List<SubscriptionRevenueTierRow> rows = new List<SubscriptionRevenueTierRow>();
            while (await reader.ReadAsync())
            {
                rows.Add(new SubscriptionRevenueTierRow(
                    reader.GetInt32("Lvl"),
                    Convert.ToInt32(reader["Revenue"]),
                    reader.GetInt32("Cnt")));
            }
            return rows;
        });
    }
}
