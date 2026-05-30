using System.Text;

using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities.QrPayments;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.QrPayments;

public sealed class QrPaymentIdempotencyDao : MySQLBaseDao<QrPaymentIdempotency>, IQrPaymentIdempotencyDao
{
    public QrPaymentIdempotencyDao(MySqlConnection connection)
    {
        _tableName = "QrPaymentIdempotency";
        _connection = connection;
    }

    protected override QrPaymentIdempotency MapReaderToEntity()
    {
        _entity = new QrPaymentIdempotency
        {
            TenantId = _mySqlReader!.GetGuid("TenantId"),
            ExternalReference = _mySqlReader!.GetString("ExternalReference"),
            EntityId = _mySqlReader!.GetGuid("EntityId")
        };
        return _entity;
    }

    protected override List<QrPaymentIdempotency> MapReaderToEntitiesList()
    {
        _entitiesList = new List<QrPaymentIdempotency>();
        while (_mySqlReader!.Read())
        {
            _entity = new QrPaymentIdempotency
            {
                TenantId = _mySqlReader!.GetGuid("TenantId"),
                ExternalReference = _mySqlReader!.GetString("ExternalReference"),
                EntityId = _mySqlReader!.GetGuid("EntityId")
            };
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(QrPaymentIdempotency record) =>
        throw new NotSupportedException("Use TryRecordAsync(QrPaymentIdempotency, ITransactionContext).");

    protected override StringBuilder UpdateCommandIntoStringBuilder(QrPaymentIdempotency record) =>
        throw new NotSupportedException("QrPaymentIdempotency rows are immutable.");

    public async Task<bool> TryRecordAsync(QrPaymentIdempotency idempotencyRecord, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO QrPaymentIdempotency (TenantId, ExternalReference, EntityId) " +
                           "VALUES (@tenantId, @externalReference, @entityId);";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        insertCommand.Parameters.AddWithValue("@tenantId", idempotencyRecord.TenantId.ToString());
        insertCommand.Parameters.AddWithValue("@externalReference", idempotencyRecord.ExternalReference);
        insertCommand.Parameters.AddWithValue("@entityId", idempotencyRecord.EntityId.ToString());

        try
        {
            await insertCommand.ExecuteNonQueryAsync();
            return true;
        }
        catch (MySqlException duplicateKeyException) when (duplicateKeyException.Number == 1062)
        {
            return false;
        }
    }

    public async Task<QrPaymentIdempotency?> GetByExternalReferenceAsync(Guid tenantId, string externalReference)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT TenantId, ExternalReference, EntityId " +
                               "FROM QrPaymentIdempotency " +
                               "WHERE TenantId = @tenantId AND ExternalReference = @externalReference;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters.AddWithValue("@externalReference", externalReference);

            _mySqlReader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            if (!await _mySqlReader.ReadAsync())
            {
                _mySqlReader.Close();
                return (QrPaymentIdempotency?)null;
            }
            QrPaymentIdempotency idempotencyRecord = MapReaderToEntity();
            _mySqlReader.Close();
            return (QrPaymentIdempotency?)idempotencyRecord;
        });
    }
}
