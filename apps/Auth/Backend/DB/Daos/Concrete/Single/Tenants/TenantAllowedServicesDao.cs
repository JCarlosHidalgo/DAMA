using System.Data;

using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.Entities.Tenants;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.Tenants;

public sealed class TenantAllowedServicesDao : ITenantAllowedServicesDao
{
    private readonly MySqlConnection _connection;

    public TenantAllowedServicesDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<TenantAllowedServices?> ReadByTenantIdAsync(Guid tenantId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand command = new MySqlCommand("GetTenantAllowedServices", _connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            command.Parameters["@tenantId"].Direction = ParameterDirection.Input;

            MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                await reader.CloseAsync();
                return (TenantAllowedServices?)null;
            }

            TenantAllowedServices allowedServices = new TenantAllowedServices
            {
                Id = reader.GetGuid("Id"),
                IndexCoreServicesPyramid = reader.GetInt32("IndexCoreServicesPyramid"),
                ExpiresAt = reader.GetDateTime("ExpiresAt")
            };

            await reader.CloseAsync();
            return (TenantAllowedServices?)allowedServices;
        });
    }

    public async Task UpsertAsync(TenantAllowedServices allowedServices, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        MySqlCommand command = new MySqlCommand("UpsertTenantAllowedServices", _connection, sqlTransaction)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@tenantId", allowedServices.Id.ToString());
        command.Parameters["@tenantId"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@indexCoreServicesPyramid", allowedServices.IndexCoreServicesPyramid);
        command.Parameters["@indexCoreServicesPyramid"].Direction = ParameterDirection.Input;
        command.Parameters.AddWithValue("@expiresAt", allowedServices.ExpiresAt);
        command.Parameters["@expiresAt"].Direction = ParameterDirection.Input;

        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> ResetExpiredAsync(DateTime asOf)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand command = new MySqlCommand("ResetExpiredTenantAllowedServices", _connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@asOf", asOf);
            command.Parameters["@asOf"].Direction = ParameterDirection.Input;

            return await command.ExecuteNonQueryAsync();
        });
    }
}
