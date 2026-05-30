using System.Data;
using System.Text;

using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.Entities.Courses;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Courses;

public sealed class CourseDao : MySQLSingleDao<Course>, ICourseDao
{
    public CourseDao(MySqlConnection connection)
    {
        _tableName = "Course";
        _connection = connection;
    }

    protected override Course MapReaderToEntity()
    {
        _entity = new Course
        {
            Id = _mySqlReader!.GetGuid("Id"),
            Name = _mySqlReader!.GetString("Name"),
            TenantId = _mySqlReader!.GetGuid("TenantId")
        };
        return _entity;
    }

    protected override List<Course> MapReaderToEntitiesList()
    {
        _entitiesList = new List<Course>();
        while (_mySqlReader!.Read())
        {
            _entity = new Course
            {
                Id = _mySqlReader!.GetGuid("Id"),
                Name = _mySqlReader!.GetString("Name"),
                TenantId = _mySqlReader!.GetGuid("TenantId")
            };
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(Course course) =>
        throw new NotSupportedException("Use CreateAsync(Course).");

    protected override StringBuilder UpdateCommandIntoStringBuilder(Course course) =>
        throw new NotSupportedException("CourseDao does not support generic update.");

    public new async Task CreateAsync(Course course)
    {
        await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "INSERT INTO Course (Id, Name, TenantId) " +
                               "VALUES (@Id, @Name, @TenantId);";
            MySqlCommand com = new MySqlCommand(sql, _connection);
            com.Parameters.AddWithValue("@Id", course.Id);
            com.Parameters.AddWithValue("@Name", course.Name);
            com.Parameters.AddWithValue("@TenantId", course.TenantId);

            await com.ExecuteNonQueryAsync();
        });
    }

    public async Task CreateAsync(Course course, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO Course (Id, Name, TenantId) " +
                           "VALUES (@Id, @Name, @TenantId);";
        MySqlCommand com = new MySqlCommand(sql, _connection, sqlTransaction);
        com.Parameters.AddWithValue("@Id", course.Id);
        com.Parameters.AddWithValue("@Name", course.Name);
        com.Parameters.AddWithValue("@TenantId", course.TenantId);

        await com.ExecuteNonQueryAsync();
    }

    public async Task<List<Course>> GetCoursesByTenantIdAsync(Guid tenantId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetCoursesByTenantId");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<Course?> GetByIdForTenantAsync(Guid tenantId, Guid courseId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetCourseByIdForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@courseId", courseId.ToString());
            com.Parameters["@courseId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            if (!await _mySqlReader.ReadAsync())
            {
                _mySqlReader.Close();
                return null;
            }
            Course course = MapReaderToEntity();
            _mySqlReader.Close();
            return (Course?)course;
        });
    }

    public async Task<bool> ExistsForTenantAsync(Guid tenantId, Guid courseId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("ExistsCourseForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@courseId", courseId.ToString());
            com.Parameters["@courseId"].Direction = ParameterDirection.Input;

            object? scalar = await com.ExecuteScalarAsync();
            return scalar != null;
        });
    }

    public async Task<bool> UpdateForTenantAsync(Guid tenantId, Guid courseId, string newName)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("UpdateCourseForTenant");
            com.Parameters.AddWithValue("@courseId", courseId.ToString());
            com.Parameters["@courseId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@newName", newName);
            com.Parameters["@newName"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;

            object? scalar = await com.ExecuteScalarAsync();
            return Convert.ToInt64(scalar) > 0;
        });
    }

    public async Task<bool> DeleteForTenantAsync(Guid tenantId, Guid courseId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);

        await ExecuteScopedDeleteAsync(
            sqlTransaction,
            "DELETE FROM ScheduledClass WHERE CourseId = @courseId AND TenantId = @tenantId;",
            tenantId,
            courseId);
        await ExecuteScopedDeleteAsync(
            sqlTransaction,
            "DELETE FROM UniqueClass WHERE CourseId = @courseId AND TenantId = @tenantId;",
            tenantId,
            courseId);

        const string deleteCourseSql = "DELETE FROM Course WHERE Id = @courseId AND TenantId = @tenantId;";
        MySqlCommand deleteCourseCommand = new MySqlCommand(deleteCourseSql, _connection, sqlTransaction);
        deleteCourseCommand.Parameters.AddWithValue("@courseId", courseId.ToString());
        deleteCourseCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        int affected = await deleteCourseCommand.ExecuteNonQueryAsync();
        return affected > 0;
    }

    private async Task ExecuteScopedDeleteAsync(MySqlTransaction transaction, string sql, Guid tenantId, Guid courseId)
    {
        MySqlCommand command = new MySqlCommand(sql, _connection, transaction);
        command.Parameters.AddWithValue("@courseId", courseId.ToString());
        command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        await command.ExecuteNonQueryAsync();
    }
}
