using System.Data;
using System.Text;

using Backend.DB.Daos.Abstract.Single.Scheduleds;
using Backend.Entities;
using Backend.Entities.Scheduleds;
using Backend.Mapping;
using Backend.Results;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Scheduleds;

public sealed class ScheduledClassDao : MySQLSingleDao<ScheduledClass>, IScheduledClassDao
{
    public ScheduledClassDao(MySqlConnection connection)
    {
        _tableName = "ScheduledClass";
        _connection = connection;
    }

    protected override ScheduledClass MapReaderToEntity()
    {
        _entity = ReadCurrentRow();
        return _entity;
    }

    protected override List<ScheduledClass> MapReaderToEntitiesList()
    {
        _entitiesList = new List<ScheduledClass>();
        while (_mySqlReader!.Read())
        {
            _entity = ReadCurrentRow();
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    private ScheduledClass ReadCurrentRow()
    {
        TimeSpan rawStartTime = _mySqlReader!.GetTimeSpan("StartTime");
        TimeSpan rawEndTime = _mySqlReader!.GetTimeSpan("EndTime");
        return new ScheduledClass
        {
            Id = _mySqlReader!.GetGuid("Id"),
            DayOfWeekIndex = _mySqlReader!.GetInt32("DayOfWeekIndex"),
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

    protected override StringBuilder CreateCommandIntoStringBuilder(ScheduledClass scheduledClass) =>
        throw new NotSupportedException("ScheduledClassDao does not support generic create.");

    protected override StringBuilder UpdateCommandIntoStringBuilder(ScheduledClass scheduledClass) =>
        throw new NotSupportedException("ScheduledClassDao does not support generic update.");

    public async Task<bool> CreateForTenantAsync(ScheduledClass scheduledClass, Guid tenantId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand com = new MySqlCommand("CreateScheduledClass", _connection, sqlTransaction) { CommandType = CommandType.StoredProcedure };
        com.Parameters.AddWithValue("@id", scheduledClass.Id.ToString());
        com.Parameters["@id"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@dayOfWeekIndex", scheduledClass.DayOfWeekIndex);
        com.Parameters["@dayOfWeekIndex"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@maxStudentLimit", scheduledClass.MaxStudentLimit);
        com.Parameters["@maxStudentLimit"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@startTime", scheduledClass.StartTime.ToTimeSpan());
        com.Parameters["@startTime"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@endTime", scheduledClass.EndTime.ToTimeSpan());
        com.Parameters["@endTime"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@courseId", scheduledClass.CourseId.ToString());
        com.Parameters["@courseId"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@groupId", scheduledClass.GroupId.ToString());
        com.Parameters["@groupId"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
        object? scalar = await com.ExecuteScalarAsync();
        return Convert.ToInt64(scalar) > 0;
    }

    public async Task InsertTeacherAsync(Guid scheduledClassId, ClassTeacher teacher, Guid tenantId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand com = new MySqlCommand("InsertScheduledClassTeacher", _connection, sqlTransaction) { CommandType = CommandType.StoredProcedure };
        com.Parameters.AddWithValue("@scheduledClassId", scheduledClassId.ToString());
        com.Parameters["@scheduledClassId"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@teacherId", teacher.TeacherId.ToString());
        com.Parameters["@teacherId"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@teacherName", teacher.TeacherName);
        com.Parameters["@teacherName"].Direction = ParameterDirection.Input;
        com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
        await com.ExecuteNonQueryAsync();
    }

    public async Task ReplaceTeachersAsync(Guid scheduledClassId, IReadOnlyList<ClassTeacher> teachers, Guid tenantId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand del = new MySqlCommand("DeleteScheduledClassTeachers", _connection, sqlTransaction) { CommandType = CommandType.StoredProcedure };
        del.Parameters.AddWithValue("@scheduledClassId", scheduledClassId.ToString());
        del.Parameters["@scheduledClassId"].Direction = ParameterDirection.Input;
        await del.ExecuteNonQueryAsync();

        foreach (ClassTeacher teacher in teachers)
        {
            await InsertTeacherAsync(scheduledClassId, teacher, tenantId, transaction);
        }
    }

    public async Task<ScheduledClass?> GetByIdForTenantAsync(Guid tenantId, Guid id)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetScheduledClassByIdForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@id", id.ToString());
            com.Parameters["@id"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            if (!await _mySqlReader.ReadAsync())
            {
                _mySqlReader.Close();
                return (ScheduledClass?)null;
            }
            ScheduledClass entity = MapReaderToEntity();
            _mySqlReader.Close();
            return (ScheduledClass?)entity;
        });
    }

    public async Task<List<ScheduledClass>> GetScheduledClassesByCourseIdAsync(Guid courseId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetScheduledClassesByCourseId");
            com.Parameters.AddWithValue("@courseId", courseId.ToString());
            com.Parameters["@courseId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<bool> UpdateForTenantAsync(ScheduledClassUpdate scheduledClassUpdate, Guid tenantId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand command = new MySqlCommand("UpdateScheduledClassForTenant", _connection, sqlTransaction) { CommandType = CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@id", scheduledClassUpdate.Id.ToString());
        command.Parameters["@id"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@dayOfWeekIndex", scheduledClassUpdate.DayOfWeekIndex);
        command.Parameters["@dayOfWeekIndex"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@maxStudentLimit", scheduledClassUpdate.MaxStudentLimit);
        command.Parameters["@maxStudentLimit"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@startTime", scheduledClassUpdate.StartTime.ToTimeSpan());
        command.Parameters["@startTime"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@endTime", scheduledClassUpdate.EndTime.ToTimeSpan());
        command.Parameters["@endTime"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        command.Parameters["@tenantId"].Direction = ParameterDirection.Input;

        object? scalar = await command.ExecuteScalarAsync();
        return Convert.ToInt64(scalar) > 0;
    }

    public async Task<List<ScheduledClass>> GetByTenantAsync(Guid tenantId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetScheduledClassesForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<List<ScheduledClass>> GetByTeacherForTenantAsync(Guid tenantId, Guid teacherId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetScheduledClassesByTeacherForTenant");
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
        const string sql = "DELETE FROM ScheduledClass WHERE Id = @id AND TenantId = @tenantId;";
        MySqlCommand command = new MySqlCommand(sql, _connection, sqlTransaction);
        command.Parameters.AddWithValue("@id", id.ToString());
        command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        int affected = await command.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<List<Guid>> GetIdsByCourseForTenantAsync(Guid tenantId, Guid courseId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "SELECT Id FROM ScheduledClass WHERE CourseId = @courseId AND TenantId = @tenantId;";
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

    public async Task<ClassExistenceMeta?> FindForTenantAsync(Guid tenantId, Guid classId, DateOnly classDate)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("ExistsScheduledClassForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@classId", classId.ToString());
            com.Parameters["@classId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@classDate", classDate.ToString("yyyy-MM-dd"));
            com.Parameters["@classDate"].Direction = ParameterDirection.Input;

            using MySqlDataReader reader = (MySqlDataReader)await com.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            TimeOnly start = TimeOnly.FromTimeSpan(reader.GetTimeSpan("StartTime"));
            TimeOnly end = TimeOnly.FromTimeSpan(reader.GetTimeSpan("EndTime"));
            int maxStudentLimit = reader.GetInt32("MaxStudentLimit");
            return new ClassExistenceMeta(start, end, ClassDate: null, maxStudentLimit);
        });
    }

    public async Task<bool> HasGroupOverlapAsync(Guid tenantId, Guid groupId, int dayOfWeekIndex, TimeOnly startTime, TimeOnly endTime, Guid? excludeId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("HasGroupClassOverlap");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@groupId", groupId.ToString());
            com.Parameters["@groupId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@candidateKind", "Scheduled");
            com.Parameters["@candidateKind"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@dayOfWeekIndex", dayOfWeekIndex);
            com.Parameters["@dayOfWeekIndex"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@classDate", DBNull.Value);
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
        MySqlCommand com = new MySqlCommand("TransferScheduledClassToGroup", _connection, sqlTransaction) { CommandType = CommandType.StoredProcedure };
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
