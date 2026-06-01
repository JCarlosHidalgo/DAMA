using System.Data;
using System.Text;

using Backend.DB.Daos.Abstract.Single.PaymentCredentials;
using Backend.Entities.PaymentCredentials;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.PaymentCredentials;

public sealed class TenantPaymentCredentialDao : MySQLSingleDao<TenantPaymentCredential>,
                                                 ITenantPaymentCredentialReader,
                                                 ITenantPaymentCredentialWriter
{
    public TenantPaymentCredentialDao(MySqlConnection connection)
    {
        _tableName = "TenantPaymentCredentials";
        _connection = connection;
    }

    protected override TenantPaymentCredential MapReaderToEntity()
    {
        _entity = new TenantPaymentCredential
        {
            TenantId = _mySqlReader!.GetGuid("TenantId"),
            TodotixAppKey = _mySqlReader!.GetString("TodotixAppKey")
        };
        return _entity;
    }

    protected override List<TenantPaymentCredential> MapReaderToEntitiesList()
    {
        _entitiesList = new List<TenantPaymentCredential>();
        while (_mySqlReader!.Read())
        {
            _entity = new TenantPaymentCredential
            {
                TenantId = _mySqlReader!.GetGuid("TenantId"),
                TodotixAppKey = _mySqlReader!.GetString("TodotixAppKey")
            };
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(TenantPaymentCredential credential) =>
        throw new NotSupportedException("Use UpsertAsync.");

    protected override StringBuilder UpdateCommandIntoStringBuilder(TenantPaymentCredential credential) =>
        throw new NotSupportedException("Use UpsertAsync.");

    public async Task<TenantPaymentCredential?> GetByTenantAsync(Guid tenantId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = GetCommandStoredProcedure("GetPaymentCredentialsByTenant");
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            if (!await _mySqlReader.ReadAsync())
            {
                _mySqlReader.Close();
                return null;
            }
            TenantPaymentCredential credential = MapReaderToEntity();
            _mySqlReader.Close();
            return (TenantPaymentCredential?)credential;
        });
    }

    public async Task UpsertAsync(Guid tenantId, string todotixAppKey)
    {
        await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand upsertCommand = GetCommandStoredProcedure("UpsertPaymentCredentialsForTenant");
            upsertCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            upsertCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            upsertCommand.Parameters.AddWithValue("@todotixAppKey", todotixAppKey);
            upsertCommand.Parameters["@todotixAppKey"].Direction = ParameterDirection.Input;

            await upsertCommand.ExecuteScalarAsync();
            return true;
        });
    }
}
