using System.Text;

using Backend.DB.Daos.Abstract.Single.Remain;
using Backend.Entities.Remain;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Remain;

public sealed class StudentRemainClassesDao : MySQLSingleDao<StudentRemainClasses>, IStudentRemainClassesDao
{
    public StudentRemainClassesDao(MySqlConnection connection)
    {
        _tableName = "StudentRemainClasses";
        _connection = connection;
    }

    protected override StudentRemainClasses MapReaderToEntity()
    {
        _entity = ReadCurrentRow();
        return _entity;
    }

    protected override List<StudentRemainClasses> MapReaderToEntitiesList()
    {
        _entitiesList = new List<StudentRemainClasses>();
        while (_mySqlReader!.Read())
        {
            _entity = ReadCurrentRow();
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    private StudentRemainClasses ReadCurrentRow()
    {
        int studentNameOrdinal = _mySqlReader!.GetOrdinal("StudentName");
        return new StudentRemainClasses
        {
            TenantId = _mySqlReader!.GetGuid("TenantId"),
            Id = _mySqlReader!.GetGuid("Id"),
            NumberOfClasses = _mySqlReader!.GetInt32("NumberOfClasses"),
            StudentName = _mySqlReader!.IsDBNull(studentNameOrdinal)
                            ? null
                            : _mySqlReader!.GetString(studentNameOrdinal)
        };
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(StudentRemainClasses remain)
    {
        throw new NotSupportedException("StudentRemainClassesDao does not support generic create.");
    }

    protected override StringBuilder UpdateCommandIntoStringBuilder(StudentRemainClasses remain)
    {
        throw new NotSupportedException("Use IncrementAsync(Guid, int) for atomic counter updates.");
    }

    public async Task IncrementAsync(
        Guid tenantId,
        Guid studentId,
        int delta,
        string? studentName,
        ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO StudentRemainClasses (TenantId, Id, NumberOfClasses, StudentName) " +
                           "VALUES (@tenantId, @studentId, @delta, @studentName) " +
                           "ON DUPLICATE KEY UPDATE " +
                           "    NumberOfClasses = NumberOfClasses + @delta, " +
                           "    StudentName = COALESCE(@studentName, StudentName);";
        MySqlCommand command = new MySqlCommand(sql, _connection, sqlTransaction);
        command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        command.Parameters.AddWithValue("@studentId", studentId.ToString());
        command.Parameters.AddWithValue("@delta", delta);
        command.Parameters.AddWithValue("@studentName", (object?)studentName ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> IncrementAllInTenantAsync(Guid tenantId, int delta, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "UPDATE StudentRemainClasses " +
                           "SET NumberOfClasses = NumberOfClasses + @delta " +
                           "WHERE TenantId = @tenantId;";
        MySqlCommand command = new MySqlCommand(sql, _connection, sqlTransaction);
        command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        command.Parameters.AddWithValue("@delta", delta);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> TryDecrementAsync(Guid tenantId, Guid studentId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "UPDATE StudentRemainClasses " +
                           "SET NumberOfClasses = NumberOfClasses - 1 " +
                           "WHERE TenantId = @tenantId AND Id = @studentId AND NumberOfClasses > 0;";
        MySqlCommand command = new MySqlCommand(sql, _connection, sqlTransaction);
        command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        command.Parameters.AddWithValue("@studentId", studentId.ToString());

        int affectedRowCount = await command.ExecuteNonQueryAsync();
        return affectedRowCount > 0;
    }

    public async Task<StudentRemainClasses?> ReadAsync(Guid tenantId, Guid studentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT TenantId, Id, NumberOfClasses, StudentName FROM StudentRemainClasses " +
                               "WHERE TenantId = @tenantId AND Id = @studentId LIMIT 1;";
            MySqlCommand command = new MySqlCommand(sql, _connection);
            command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            command.Parameters.AddWithValue("@studentId", studentId.ToString());

            _mySqlReader = (MySqlDataReader)await command.ExecuteReaderAsync();
            StudentRemainClasses? entity = null;
            if (await _mySqlReader.ReadAsync())
            {
                entity = MapReaderToEntity();
            }
            await _mySqlReader.CloseAsync();
            return entity;
        });
    }
}
