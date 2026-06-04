using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.Entities.Subscriptions;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Subscriptions;

public sealed class SubscriptionPlanDao : ISubscriptionPlanDao
{
    private readonly MySqlConnection _connection;

    public SubscriptionPlanDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<SubscriptionPlan?> GetByLevelAsync(int level)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT Level, Price, Currency, DurationAmount, DurationUnit, UpdatedAt " +
                               "FROM SubscriptionPlan WHERE Level = @level;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
            selectCommand.Parameters.AddWithValue("@level", level);

            using MySqlDataReader reader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapReader(reader) : null;
        });
    }

    public async Task<List<SubscriptionPlan>> GetAllAsync()
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT Level, Price, Currency, DurationAmount, DurationUnit, UpdatedAt " +
                               "FROM SubscriptionPlan ORDER BY Level ASC;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);

            using MySqlDataReader reader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            List<SubscriptionPlan> plans = new List<SubscriptionPlan>();
            while (await reader.ReadAsync())
            {
                plans.Add(MapReader(reader));
            }
            return plans;
        });
    }

    public async Task UpsertAsync(SubscriptionPlan plan)
    {
        await MySQLRetryPolicy.EnsureOpenAsync(_connection);
        const string sql = "INSERT INTO SubscriptionPlan (Level, Price, Currency, DurationAmount, DurationUnit, UpdatedAt) " +
                           "VALUES (@Level, @Price, @Currency, @DurationAmount, @DurationUnit, NOW(6)) " +
                           "ON DUPLICATE KEY UPDATE " +
                           "Price = VALUES(Price), Currency = VALUES(Currency), DurationAmount = VALUES(DurationAmount), " +
                           "DurationUnit = VALUES(DurationUnit), UpdatedAt = NOW(6);";
        MySqlCommand upsertCommand = new MySqlCommand(sql, _connection);
        upsertCommand.Parameters.AddWithValue("@Level", plan.Level);
        upsertCommand.Parameters.AddWithValue("@Price", plan.Price);
        upsertCommand.Parameters.AddWithValue("@Currency", plan.Currency);
        upsertCommand.Parameters.AddWithValue("@DurationAmount", plan.DurationAmount);
        upsertCommand.Parameters.AddWithValue("@DurationUnit", plan.DurationUnit);
        await upsertCommand.ExecuteNonQueryAsync();
    }

    private static SubscriptionPlan MapReader(MySqlDataReader reader)
    {
        return new SubscriptionPlan
        {
            Level = reader.GetInt32("Level"),
            Price = reader.GetInt32("Price"),
            Currency = reader.GetString("Currency"),
            DurationAmount = reader.GetInt32("DurationAmount"),
            DurationUnit = reader.GetString("DurationUnit"),
            UpdatedAt = reader.GetDateTime("UpdatedAt")
        };
    }
}
