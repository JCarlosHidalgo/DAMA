using System.Data;
using System.Text;

using Backend.DB.Daos.Abstract.Single.Uniques;
using Backend.Entities;
using Backend.Entities.Uniques;
using Backend.Mapping;
using Backend.Results;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Uniques;

public sealed class UniqueClassDao : MySQLSingleDao<UniqueClass>, IUniqueClassDao
{
    public UniqueClassDao(MySqlConnection connection)
    {
        _tableName = "UniqueClass";
        _connection = connection;
    }

    protected override UniqueClass MapReaderToEntity()
    {
        _entity = ReadCurrentRow();
        return _entity;
    }

    protected override List<UniqueClass> MapReaderToEntitiesList()
    {
        _entitiesList = new List<UniqueClass>();
        while (_mySqlReader!.Read())
        {
            _entity = ReadCurrentRow();
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    private UniqueClass ReadCurrentRow()
    {
        DateTime rawDate = _mySqlReader!.GetDateTime("Date");
        TimeSpan rawStartTime = _mySqlReader!.GetTimeSpan("StartTime");
        TimeSpan rawEndTime = _mySqlReader!.GetTimeSpan("EndTime");
        return new UniqueClass
        {
            Id = _mySqlReader!.GetGuid("Id"),
            Date = DateOnly.FromDateTime(rawDate),
            MaxStudentLimit = _mySqlReader!.GetInt32("MaxStudentLimit"),
            StartTime = TimeOnly.FromTimeSpan(rawStartTime),
            EndTime = TimeOnly.FromTimeSpan(rawEndTime),
            CourseId = _mySqlReader!.GetGuid("CourseId"),
            GroupId = _mySqlReader!.GetGuid("GroupId"),
            TenantId = _mySqlReader!.GetGuid("TenantId"),
            GroupName = _mySqlReader!.GetString("GroupName"),
            Teachers = ClassTeachersJsonParser.Parse(_mySqlReader!.GetString("Teachers"))
        };
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(UniqueClass uniqueClass) =>
        throw new NotSupportedException("UniqueClassDao does not support generic create.");

    protected override StringBuilder UpdateCommandIntoStringBuilder(UniqueClass uniqueClass) =>
        throw new NotSupportedException("UniqueClassDao does not support generic update.");

    public async Task<bool> CreateForTenantAsync(UniqueClass uniqueClass, Guid tenantId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand com = new MySqlCommand("CreateUniqueClass", _connection, sqlTransaction) { CommandType = CommandType.StoredProcedure };
        com.Parameters.AddWithValue("@id", uniqueClass.Id.ToString());
        com.Parameters["@id"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@classDate", uniqueClass.Date.ToString("yyyy-MM-dd"));
        com.Parameters["@classDate"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@maxStudentLimit", uniqueClass.MaxStudentLimit);
        com.Parameters["@maxStudentLimit"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@startTime", uniqueClass.StartTime.ToTimeSpan());
        com.Parameters["@startTime"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@endTime", uniqueClass.EndTime.ToTimeSpan());
        com.Parameters["@endTime"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@courseId", uniqueClass.CourseId.ToString());
        com.Parameters["@courseId"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@groupId", uniqueClass.GroupId.ToString());
        com.Parameters["@groupId"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
        object? scalar = await com.ExecuteScalarAsync();
        return Convert.ToInt64(scalar) > 0;
    }

    public async Task InsertTeacherAsync(Guid uniqueClassId, ClassTeacher teacher, Guid tenantId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand com = new MySqlCommand("InsertUniqueClassTeacher", _connection, sqlTransaction) { CommandType = CommandType.StoredProcedure };
        com.Parameters.AddWithValue("@uniqueClassId", uniqueClassId.ToString());
        com.Parameters["@uniqueClassId"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@teacherId", teacher.TeacherId.ToString());
        com.Parameters["@teacherId"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@teacherName", teacher.TeacherName);
        com.Parameters["@teacherName"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
        await com.ExecuteNonQueryAsync();
    }

    public async Task ReplaceTeachersAsync(Guid uniqueClassId, IReadOnlyList<ClassTeacher> teachers, Guid tenantId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand del = new MySqlCommand("DeleteUniqueClassTeachers", _connection, sqlTransaction) { CommandType = CommandType.StoredProcedure };
        del.Parameters.AddWithValue("@uniqueClassId", uniqueClassId.ToString());
        del.Parameters["@uniqueClassId"].Direction = ParameterDirection.Input;
        await del.ExecuteNonQueryAsync();

        foreach (ClassTeacher teacher in teachers)
        {
            await InsertTeacherAsync(uniqueClassId, teacher, tenantId, transaction);
        }
    }

    public async Task<UniqueClass?> GetByIdForTenantAsync(Guid tenantId, Guid id)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetUniqueClassByIdForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@id", id.ToString());
            com.Parameters["@id"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            if (!await _mySqlReader.ReadAsync())
            {
                _mySqlReader.Close();
                return (UniqueClass?)null;
            }
            UniqueClass entity = MapReaderToEntity();
            _mySqlReader.Close();
            return (UniqueClass?)entity;
        });
    }

    public async Task<List<UniqueClass>> GetUniqueClassesOnSameWeekByDateAsync(Guid courseId, DateOnly classDate)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetUniqueClassesOnSameWeekByDate");
            com.Parameters.AddWithValue("@dateInput", classDate.ToString("yyyy-MM-dd"));
            com.Parameters["@dateInput"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@courseId", courseId.ToString());
            com.Parameters["@courseId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<bool> UpdateForTenantAsync(UniqueClassUpdate uniqueClassUpdate, Guid tenantId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand command = new MySqlCommand("UpdateUniqueClassForTenant", _connection, sqlTransaction) { CommandType = CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@id", uniqueClassUpdate.Id.ToString());
        command.Parameters["@id"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@classDate", uniqueClassUpdate.Date.ToString("yyyy-MM-dd"));
        command.Parameters["@classDate"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@maxStudentLimit", uniqueClassUpdate.MaxStudentLimit);
        command.Parameters["@maxStudentLimit"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@startTime", uniqueClassUpdate.StartTime.ToTimeSpan());
        command.Parameters["@startTime"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@endTime", uniqueClassUpdate.EndTime.ToTimeSpan());
        command.Parameters["@endTime"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        command.Parameters["@tenantId"].Direction = ParameterDirection.Input;

        object? scalar = await command.ExecuteScalarAsync();
        return Convert.ToInt64(scalar) > 0;
    }

    public async Task<List<UniqueClass>> GetOnWeekForTenantAsync(Guid tenantId, DateOnly classDate)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetUniqueClassesOnWeekForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@dateInput", classDate.ToString("yyyy-MM-dd"));
            com.Parameters["@dateInput"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<List<UniqueClass>> GetByTeacherOnWeekForTenantAsync(Guid tenantId, Guid teacherId, DateOnly classDate)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetUniqueClassesOnWeekByTeacherForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@teacherId", teacherId.ToString());
            com.Parameters["@teacherId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@dateInput", classDate.ToString("yyyy-MM-dd"));
            com.Parameters["@dateInput"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<List<UniqueClass>> GetByTeacherForTenantAsync(Guid tenantId, Guid teacherId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetUniqueClassesByTeacherForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@teacherId", teacherId.ToString());
            com.Parameters["@teacherId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<bool> DeleteForTenantAsync(Guid tenantId, Guid id, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "DELETE FROM UniqueClass WHERE Id = @id AND TenantId = @tenantId;";
        MySqlCommand command = new MySqlCommand(sql, _connection, sqlTransaction);
        command.Parameters.AddWithValue("@id", id.ToString());
        command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        int affected = await command.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<List<Guid>> GetIdsByCourseForTenantAsync(Guid tenantId, Guid courseId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "SELECT Id FROM UniqueClass WHERE CourseId = @courseId AND TenantId = @tenantId;";
        MySqlCommand command = new MySqlCommand(sql, _connection, sqlTransaction);
        command.Parameters.AddWithValue("@courseId", courseId.ToString());
        command.Parameters.AddWithValue("@tenantId", tenantId.ToString());

        List<Guid> ids = new List<Guid>();
        using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetGuid("Id"));
        }
        return ids;
    }

    public async Task<ClassExistenceMeta?> FindForTenantAsync(Guid tenantId, Guid classId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("ExistsUniqueClassForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@classId", classId.ToString());
            com.Parameters["@classId"].Direction = ParameterDirection.Input;

            using MySqlDataReader reader = (MySqlDataReader)await com.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            TimeOnly start = TimeOnly.FromTimeSpan(reader.GetTimeSpan("StartTime"));
            TimeOnly end = TimeOnly.FromTimeSpan(reader.GetTimeSpan("EndTime"));
            DateOnly date = DateOnly.FromDateTime(reader.GetDateTime("ClassDate"));
            int maxStudentLimit = reader.GetInt32("MaxStudentLimit");
            return new ClassExistenceMeta(start, end, date, maxStudentLimit);
        });
    }

    public async Task<bool> HasGroupOverlapAsync(Guid tenantId, Guid groupId, DateOnly classDate, TimeOnly startTime, TimeOnly endTime, Guid? excludeId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            int dayOfWeekIndex = (int)classDate.DayOfWeek == 0 ? 7 : (int)classDate.DayOfWeek;
            MySqlCommand com = GetCommandStoredProcedure("HasGroupClassOverlap");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@groupId", groupId.ToString());
            com.Parameters["@groupId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@candidateKind", "Unique");
            com.Parameters["@candidateKind"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@dayOfWeekIndex", dayOfWeekIndex);
            com.Parameters["@dayOfWeekIndex"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@classDate", classDate.ToString("yyyy-MM-dd"));
            com.Parameters["@classDate"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@startTime", startTime.ToTimeSpan());
            com.Parameters["@startTime"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@endTime", endTime.ToTimeSpan());
            com.Parameters["@endTime"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@excludeId", excludeId.HasValue ? (object)excludeId.Value.ToString() : DBNull.Value);
            com.Parameters["@excludeId"].Direction = ParameterDirection.Input;

            object? scalar = await com.ExecuteScalarAsync();
            return Convert.ToInt64(scalar) > 0;
        });
    }

    public async Task<bool> TransferToGroupAsync(Guid tenantId, Guid id, Guid targetGroupId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand com = new MySqlCommand("TransferUniqueClassToGroup", _connection, sqlTransaction) { CommandType = CommandType.StoredProcedure };
        com.Parameters.AddWithValue("@id", id.ToString());
        com.Parameters["@id"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@targetGroupId", targetGroupId.ToString());
        com.Parameters["@targetGroupId"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
        object? scalar = await com.ExecuteScalarAsync();
        return Convert.ToInt64(scalar) > 0;
    }
}
