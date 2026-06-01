using System.Data;
using System.Text;

using Backend.DB.Daos.Abstract.Single.Groups;
using Backend.Entities.Groups;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Groups;

public sealed class ClassGroupDao : MySQLSingleDao<ClassGroup>, IClassGroupDao
{
    public ClassGroupDao(MySqlConnection connection)
    {
        _tableName = "ClassGroup";
        _connection = connection;
    }

    protected override ClassGroup MapReaderToEntity()
    {
        _entity = ReadCurrentRow();
        return _entity;
    }

    protected override List<ClassGroup> MapReaderToEntitiesList()
    {
        _entitiesList = new List<ClassGroup>();
        while (_mySqlReader!.Read())
        {
            _entitiesList.Add(ReadCurrentRow());
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    private ClassGroup ReadCurrentRow()
    {
        return new ClassGroup
        {
            Id = _mySqlReader!.GetGuid("Id"),
            Name = _mySqlReader!.GetString("Name"),
            TenantId = _mySqlReader!.GetGuid("TenantId")
        };
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(ClassGroup classGroup) =>
        throw new NotSupportedException("Use CreateForTenantAsync(ClassGroup).");

    protected override StringBuilder UpdateCommandIntoStringBuilder(ClassGroup classGroup) =>
        throw new NotSupportedException("ClassGroupDao does not support generic update.");

    public async Task CreateForTenantAsync(ClassGroup classGroup)
    {
        await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("CreateClassGroup");
            com.Parameters.AddWithValue("@id", classGroup.Id.ToString());
            com.Parameters["@id"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@name", classGroup.Name);
            com.Parameters["@name"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@tenantId", classGroup.TenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            await com.ExecuteScalarAsync();
        });
    }

    public async Task<bool> UpdateForTenantAsync(Guid tenantId, Guid id, string newName)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("UpdateClassGroupForTenant");
            com.Parameters.AddWithValue("@id", id.ToString());
            com.Parameters["@id"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@newName", newName);
            com.Parameters["@newName"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            object? scalar = await com.ExecuteScalarAsync();
            return Convert.ToInt64(scalar) > 0;
        });
    }

    public async Task<bool> DeleteForTenantIfEmptyAsync(Guid tenantId, Guid id)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("DeleteClassGroupForTenant");
            com.Parameters.AddWithValue("@id", id.ToString());
            com.Parameters["@id"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            object? scalar = await com.ExecuteScalarAsync();
            return Convert.ToInt64(scalar) > 0;
        });
    }

    public async Task<List<ClassGroup>> GetByTenantAsync(Guid tenantId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetClassGroupsForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<List<ClassGroup>> GetByTeacherForTenantAsync(Guid tenantId, Guid teacherId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetClassGroupsByTeacherForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@teacherId", teacherId.ToString());
            com.Parameters["@teacherId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<bool> ExistsForTenantAsync(Guid tenantId, Guid id)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("ExistsClassGroupForTenant");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@groupId", id.ToString());
            com.Parameters["@groupId"].Direction = ParameterDirection.Input;

            object? scalar = await com.ExecuteScalarAsync();
            return scalar != null;
        });
    }
}
