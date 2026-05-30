using System.Text;

using Backend.DB.Daos.Abstract.TwoForeign.Tenants;
using Backend.Entities.Tenants;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.TwoForeign.Tenants;

public sealed class TenantDomainDao : MySQLTwoForeignDao<TenantDomain>, ITenantDomainDao
{
    public TenantDomainDao(MySqlConnection connection)
    {
        _tableName = "TenantDomain";
        _connection = connection;
    }

    protected override TenantDomain MapReaderToEntity()
    {
        _entity = new TenantDomain
        {
            UserId = _mySqlReader!.GetGuid("UserId"),
            TenantId = _mySqlReader!.GetGuid("TenantId")
        };
        _mySqlReader.Close();
        return _entity;
    }

    protected override List<TenantDomain> MapReaderToEntitiesList() =>
        throw new NotSupportedException("TenantDomainDao does not support bulk reads.");

    protected override StringBuilder CreateCommandIntoStringBuilder(TenantDomain tenantDomain) =>
        throw new NotSupportedException("Use CreateAsync(TenantDomain, ITransactionContext).");

    protected override StringBuilder UpdateCommandIntoStringBuilder(TenantDomain tenantDomain) =>
        throw new NotSupportedException("TenantDomainDao does not support generic update.");

    public async Task CreateAsync(TenantDomain domain, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO TenantDomain (UserId, TenantId) " +
                           "VALUES (@UserId, @TenantId);";
        MySqlCommand com = new MySqlCommand(sql, _connection, sqlTransaction);
        com.Parameters.AddWithValue("@UserId", domain.UserId);
        com.Parameters.AddWithValue("@TenantId", domain.TenantId);

        await com.ExecuteNonQueryAsync();
    }

}
