using System.Data;
using System.Text;

using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Entities.Todotix;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Todotix;

public sealed class TenantTodotixCredentialDao : MySQLSingleDao<TenantTodotixCredential>,
                                                 ITenantTodotixCredentialReader,
                                                 ITenantTodotixCredentialWriter
{
    public TenantTodotixCredentialDao(MySqlConnection connection)
    {
        _tableName = "TenantTodotixCredential";
        _connection = connection;
    }

    protected override TenantTodotixCredential MapReaderToEntity()
    {
        _entity = new TenantTodotixCredential
        {
            TenantId = _mySqlReader!.GetGuid("TenantId"),
            EncryptedAppKey = _mySqlReader!.GetString("EncryptedAppKey"),
            CreatedAt = _mySqlReader!.GetDateTime("CreatedAt"),
            UpdatedAt = _mySqlReader!.GetDateTime("UpdatedAt")
        };
        return _entity;
    }

    protected override List<TenantTodotixCredential> MapReaderToEntitiesList()
    {
        _entitiesList = new List<TenantTodotixCredential>();
        while (_mySqlReader!.Read())
        {
            _entity = new TenantTodotixCredential
            {
                TenantId = _mySqlReader!.GetGuid("TenantId"),
                EncryptedAppKey = _mySqlReader!.GetString("EncryptedAppKey"),
                CreatedAt = _mySqlReader!.GetDateTime("CreatedAt"),
                UpdatedAt = _mySqlReader!.GetDateTime("UpdatedAt")
            };
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(TenantTodotixCredential credential) =>
        throw new NotSupportedException("Use UpsertAsync.");

    protected override StringBuilder UpdateCommandIntoStringBuilder(TenantTodotixCredential credential) =>
        throw new NotSupportedException("Use UpsertAsync.");

    public async Task<TenantTodotixCredential?> GetByTenantAsync(Guid tenantId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = GetCommandStoredProcedure("GetTodotixCredentialByTenant");
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            if (!await _mySqlReader.ReadAsync())
            {
                _mySqlReader.Close();
                return null;
            }
            TenantTodotixCredential credential = MapReaderToEntity();
            _mySqlReader.Close();
            return (TenantTodotixCredential?)credential;
        });
    }

    public async Task UpsertAsync(Guid tenantId, string encryptedAppKey)
    {
        await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand upsertCommand = GetCommandStoredProcedure("UpsertTodotixCredentialForTenant");
            upsertCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            upsertCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            upsertCommand.Parameters.AddWithValue("@encryptedAppKey", encryptedAppKey);
            upsertCommand.Parameters["@encryptedAppKey"].Direction = ParameterDirection.Input;

            await upsertCommand.ExecuteScalarAsync();
            return true;
        });
    }
}
