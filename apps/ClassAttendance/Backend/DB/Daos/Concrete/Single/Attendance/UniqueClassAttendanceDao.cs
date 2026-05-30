using System.Data;

using Backend.DB.Daos.Abstract.Single.Attendance;
using Backend.Entities.Attendance;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Attendance;

public sealed class UniqueClassAttendanceDao : AttendanceDaoBase<UniqueClassAttendance>, IUniqueClassAttendanceDao
{
    public UniqueClassAttendanceDao(MySqlConnection connection)
    {
        _tableName = "UniqueClassAttendance";
        _connection = connection;
    }

    protected override UniqueClassAttendance BuildFromCurrentRow()
    {
        DateTime classDateAsDateTime = _mySqlReader!.GetDateTime("ClassDate");
        return new UniqueClassAttendance
        {
            TenantId = _mySqlReader!.GetGuid("TenantId"),
            ClassId = _mySqlReader!.GetGuid("ClassId"),
            ClassDate = DateOnly.FromDateTime(classDateAsDateTime),
            StartTime = TimeOnly.FromTimeSpan(_mySqlReader!.GetTimeSpan("StartTime")),
            EndTime = TimeOnly.FromTimeSpan(_mySqlReader!.GetTimeSpan("EndTime")),
            CourseName = _mySqlReader!.GetString("CourseName"),
            StudentId = _mySqlReader!.GetGuid("StudentId"),
            StudentName = _mySqlReader!.GetString("StudentName")
        };
    }

    public async Task<List<UniqueClassAttendance>> GetUniqueAttendanceAsync(Guid tenantId, Guid classId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand command = GetCommandStoredProcedure("GetUniqueAttendance");
            command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            command.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            command.Parameters.AddWithValue("@classId", classId.ToString());
            command.Parameters["@classId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await command.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<List<UniqueClassAttendance>> GetUniqueAttendanceByStudentIdAsync(Guid tenantId, Guid studentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand command = GetCommandStoredProcedure("GetUniqueAttendanceByStudentId");
            command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            command.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            command.Parameters.AddWithValue("@studentId", studentId.ToString());
            command.Parameters["@studentId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await command.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<int> CountByStudentForTenantAsync(Guid tenantId, Guid studentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand command = GetCommandStoredProcedure("CountUniqueAttendanceByStudentForTenant");
            command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            command.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            command.Parameters.AddWithValue("@studentId", studentId.ToString());
            command.Parameters["@studentId"].Direction = ParameterDirection.Input;

            object? scalarResult = await command.ExecuteScalarAsync();
            return Convert.ToInt32(scalarResult);
        });
    }

    public async Task<List<UniqueClassAttendance>> GetPageByStudentForTenantAsync(
        Guid tenantId,
        Guid studentId,
        int offset,
        int limit)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand command = GetCommandStoredProcedure("GetUniqueAttendancePageByStudentForTenant");
            command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            command.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            command.Parameters.AddWithValue("@studentId", studentId.ToString());
            command.Parameters["@studentId"].Direction = ParameterDirection.Input;
            command.Parameters.AddWithValue("@pageOffset", offset);
            command.Parameters["@pageOffset"].Direction = ParameterDirection.Input;
            command.Parameters.AddWithValue("@pageLimit", limit);
            command.Parameters["@pageLimit"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await command.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<int> CountOtherStudentsForUpdateAsync(
        Guid tenantId,
        Guid classId,
        Guid excludeStudentId,
        ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "SELECT StudentId FROM UniqueClassAttendance " +
                           "WHERE TenantId = @tenantId AND ClassId = @classId " +
                           "FOR UPDATE;";
        MySqlCommand command = new MySqlCommand(sql, _connection, sqlTransaction);
        command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        command.Parameters.AddWithValue("@classId", classId.ToString());

        HashSet<Guid> distinctOtherStudents = new HashSet<Guid>();
        using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Guid studentId = reader.GetGuid("StudentId");
            if (studentId != excludeStudentId)
            {
                distinctOtherStudents.Add(studentId);
            }
        }
        return distinctOtherStudents.Count;
    }

    public async Task<bool> TryMarkAttendanceAsync(UniqueClassAttendance attendance, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO UniqueClassAttendance " +
                           "(TenantId, ClassId, ClassDate, StartTime, EndTime, CourseName, StudentId, StudentName) " +
                           "VALUES (@tenantId, @classId, @classDate, @startTime, @endTime, @courseName, @studentId, @studentName);";
        MySqlCommand command = new MySqlCommand(sql, _connection, sqlTransaction);
        command.Parameters.AddWithValue("@tenantId", attendance.TenantId.ToString());
        command.Parameters.AddWithValue("@classId", attendance.ClassId.ToString());
        command.Parameters.AddWithValue("@classDate", attendance.ClassDate.ToString("o"));
        command.Parameters.AddWithValue("@startTime", attendance.StartTime.ToTimeSpan());
        command.Parameters.AddWithValue("@endTime", attendance.EndTime.ToTimeSpan());
        command.Parameters.AddWithValue("@courseName", attendance.CourseName);
        command.Parameters.AddWithValue("@studentId", attendance.StudentId.ToString());
        command.Parameters.AddWithValue("@studentName", attendance.StudentName);

        try
        {
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (MySqlException duplicateException) when (duplicateException.Number == 1062)
        {
            return false;
        }
    }

    public async Task<int> DeleteByClassForTenantAsync(Guid tenantId, Guid classId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "DELETE FROM UniqueClassAttendance " +
                           "WHERE TenantId = @tenantId AND ClassId = @classId;";
        MySqlCommand command = new MySqlCommand(sql, _connection, sqlTransaction);
        command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        command.Parameters.AddWithValue("@classId", classId.ToString());
        return await command.ExecuteNonQueryAsync();
    }
}
