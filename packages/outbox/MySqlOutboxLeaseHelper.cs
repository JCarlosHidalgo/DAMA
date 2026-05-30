using System.Text;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace DAMA.Software.MySqlOutbox;

public static class MySqlOutboxLeaseHelper
{
    public static async Task<List<TEvent>> LeaseAsync<TEvent>(
        MySqlConnection connection,
        OutboxLeaseDescriptor<TEvent> descriptor,
        int batchSize,
        TimeSpan leaseDuration)
        where TEvent : IOutboxEvent
    {
        var leasedEvents = new List<TEvent>();

        await MySQLRetryPolicy.EnsureOpenAsync(connection);
        await using var transaction = (MySqlTransaction)await connection.BeginTransactionAsync();
        try
        {
            string selectSql =
                "SELECT " + descriptor.SelectColumns + " " +
                "FROM " + descriptor.TableName + " " +
                "WHERE " + descriptor.PendingPredicate + " " +
                "  AND (LeasedUntil IS NULL OR LeasedUntil < NOW(6)) " +
                "ORDER BY OccurredAt " +
                "LIMIT @batchSize " +
                "FOR UPDATE SKIP LOCKED;";

            MySqlCommand selectCommand = new MySqlCommand(selectSql, connection, transaction);
            selectCommand.Parameters.AddWithValue("@batchSize", batchSize);

            using (var reader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    leasedEvents.Add(descriptor.Mapper(reader));
                }
                await reader.CloseAsync();
            }

            if (leasedEvents.Count > 0)
            {
                var leasedEventIds = new StringBuilder();
                MySqlCommand updateCommand = new MySqlCommand();
                updateCommand.Connection = connection;
                updateCommand.Transaction = transaction;
                for (int leasedEventIndex = 0; leasedEventIndex < leasedEvents.Count; leasedEventIndex++)
                {
                    string parameterName = "@id" + leasedEventIndex;
                    if (leasedEventIndex > 0)
                    {
                        leasedEventIds.Append(',');
                    }

                    leasedEventIds.Append(parameterName);
                    updateCommand.Parameters.AddWithValue(parameterName, leasedEvents[leasedEventIndex].Id.ToString());
                }
                updateCommand.CommandText =
                    "UPDATE " + descriptor.TableName + " " +
                    "SET LeasedUntil = DATE_ADD(NOW(6), INTERVAL @leaseSec SECOND), " +
                    "    Attempts = Attempts + 1 " +
                    "WHERE Id IN (" + leasedEventIds + ");";
                updateCommand.Parameters.AddWithValue("@leaseSec", (int)leaseDuration.TotalSeconds);
                await updateCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return leasedEvents;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public static async Task RecordFailureAsync(
        MySqlConnection connection,
        string tableName,
        Guid id,
        string error)
    {
        await MySQLRetryPolicy.EnsureOpenAsync(connection);
        string sql =
            "UPDATE " + tableName + " " +
            "SET LastError = @error, LeasedUntil = NULL " +
            "WHERE Id = @id;";
        MySqlCommand updateCommand = new MySqlCommand(sql, connection);
        updateCommand.Parameters.AddWithValue("@id", id.ToString());
        updateCommand.Parameters.AddWithValue("@error", error);
        await updateCommand.ExecuteNonQueryAsync();
    }
}
