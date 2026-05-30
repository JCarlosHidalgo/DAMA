using System.Data;
using System.Text;

using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.Entities.Tenants;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Tenants;

public sealed class TenantDao : MySQLSingleDao<Tenant>, ITenantDao
{
    public TenantDao(MySqlConnection connection)
    {
        _tableName = "Tenant";
        _connection = connection;
    }

    protected override Tenant MapReaderToEntity()
    {
        _entity = new Tenant
        {
            Id = _mySqlReader!.GetGuid("Id"),
            Name = _mySqlReader!.GetString("Name"),
            Timezone = _mySqlReader!.GetString("Timezone")
        };
        return _entity;
    }

    protected override List<Tenant> MapReaderToEntitiesList()
    {
        _entitiesList = new List<Tenant>();
        while (_mySqlReader!.Read())
        {
            _entity = new Tenant
            {
                Id = _mySqlReader.GetGuid("Id"),
                Name = _mySqlReader.GetString("Name"),
                Timezone = _mySqlReader.GetString("Timezone")
            };
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(Tenant tenant) =>
        throw new NotSupportedException("TenantDao does not support generic create.");

    protected override StringBuilder UpdateCommandIntoStringBuilder(Tenant tenant) =>
        throw new NotSupportedException("TenantDao does not support generic update.");

    public async Task<int> UpdateTimezoneAsync(Guid tenantId, string newTimezone)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("UpdateTenantTimezone");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@newTimezone", newTimezone);
            com.Parameters["@newTimezone"].Direction = ParameterDirection.Input;

            return await com.ExecuteNonQueryAsync();
        });
    }
}
