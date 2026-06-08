using System.Data;

using Backend.DB.Daos.Abstract.Single.QrPayments;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.QrPayments;

public sealed class StudentAnalyticsDao : IStudentAnalyticsDao
{
    private readonly MySqlConnection _connection;

    public StudentAnalyticsDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<StudentQrBreakdownRow> GetStatusBreakdownAsync(Guid tenantId, Guid studentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = new MySqlCommand("GetStudentQrStatusBreakdownForTenant", _connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@studentId", studentId.ToString());
            selectCommand.Parameters["@studentId"].Direction = ParameterDirection.Input;

            using MySqlDataReader reader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new StudentQrBreakdownRow(0, 0, 0, 0, 0, 0, 0, 0);
            }

            return new StudentQrBreakdownRow(
                reader.GetInt32("PendingCount"),
                Convert.ToInt32(reader["PendingAmount"]),
                reader.GetInt32("SuccessCount"),
                Convert.ToInt32(reader["SuccessAmount"]),
                reader.GetInt32("ExpiredCount"),
                Convert.ToInt32(reader["ExpiredAmount"]),
                reader.GetInt32("OtherFailedCount"),
                Convert.ToInt32(reader["OtherFailedAmount"]));
        });
    }

    public async Task<List<StudentSpendMonthRow>> GetSpendByMonthAsync(Guid tenantId, Guid studentId, DateTime fromDate, DateTime toDate)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = new MySqlCommand("GetStudentSpendByMonthForTenant", _connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@studentId", studentId.ToString());
            selectCommand.Parameters["@studentId"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@fromDate", fromDate);
            selectCommand.Parameters["@fromDate"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@toDate", toDate);
            selectCommand.Parameters["@toDate"].Direction = ParameterDirection.Input;

            using MySqlDataReader reader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            List<StudentSpendMonthRow> points = new List<StudentSpendMonthRow>();
            while (await reader.ReadAsync())
            {
                points.Add(new StudentSpendMonthRow(
                    reader.GetInt32("Yr"),
                    reader.GetInt32("Mo"),
                    Convert.ToInt32(reader["Amount"]),
                    reader.GetInt32("Cnt")));
            }
            return points;
        });
    }
}
