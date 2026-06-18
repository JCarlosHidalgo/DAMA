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

    public new async Task<List<Tenant>> ReadAllAsync()
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetAllTenants");

            using MySqlDataReader reader = (MySqlDataReader)await com.ExecuteReaderAsync();
            List<Tenant> tenants = new List<Tenant>();
            while (await reader.ReadAsync())
            {
                tenants.Add(new Tenant
                {
                    Id = reader.GetGuid("Id"),
                    Name = reader.GetString("Name"),
                    Timezone = reader.GetString("Timezone")
                });
            }
            return tenants;
        });
    }

    public async Task CreateTenantAsync(Tenant tenant)
    {
        await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("CreateTenant");
            com.Parameters.AddWithValue("@tenantId", tenant.Id.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@tenantName", tenant.Name);
            com.Parameters["@tenantName"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@tenantTimezone", tenant.Timezone);
            com.Parameters["@tenantTimezone"].Direction = ParameterDirection.Input;

            return await com.ExecuteNonQueryAsync();
        });
    }

    public async Task<int> UpdateNameAsync(Guid tenantId, string newName)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("UpdateTenantName");
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            com.Parameters.AddWithValue("@newName", newName);
            com.Parameters["@newName"].Direction = ParameterDirection.Input;

            return await com.ExecuteNonQueryAsync();
        });
    }

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

    public async Task<List<TenantTierCountRow>> GetCountBySubscriptionTierAsync()
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand com = GetCommandStoredProcedure("GetTenantCountBySubscriptionTier");

            using MySqlDataReader reader = (MySqlDataReader)await com.ExecuteReaderAsync();
            List<TenantTierCountRow> rows = new List<TenantTierCountRow>();
            while (await reader.ReadAsync())
            {
                rows.Add(new TenantTierCountRow(
                    reader.GetInt32("Tier"),
                    reader.GetInt32("TenantCount")));
            }
            return rows;
        });
    }
}
