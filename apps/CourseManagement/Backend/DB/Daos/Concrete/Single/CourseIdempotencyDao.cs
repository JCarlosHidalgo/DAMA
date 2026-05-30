using System.Text;

using Backend.DB.Daos.Abstract.Single;
using Backend.Entities;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single;

public sealed class CourseIdempotencyDao : MySQLBaseDao<CourseIdempotency>, ICourseIdempotencyDao
{
    public CourseIdempotencyDao(MySqlConnection connection)
    {
        _tableName = "CourseIdempotency";
        _connection = connection;
    }

    protected override CourseIdempotency MapReaderToEntity()
    {
        _entity = new CourseIdempotency
        {
            TenantId = _mySqlReader!.GetGuid("TenantId"),
            ExternalReference = _mySqlReader!.GetString("ExternalReference"),
            EntityType = _mySqlReader!.GetString("EntityType"),
            EntityId = _mySqlReader!.GetGuid("EntityId"),
            ProcessedAt = _mySqlReader!.GetDateTime("ProcessedAt")
        };
        return _entity;
    }

    protected override List<CourseIdempotency> MapReaderToEntitiesList()
    {
        _entitiesList = new List<CourseIdempotency>();
        while (_mySqlReader!.Read())
        {
            _entity = new CourseIdempotency
            {
                TenantId = _mySqlReader!.GetGuid("TenantId"),
                ExternalReference = _mySqlReader!.GetString("ExternalReference"),
                EntityType = _mySqlReader!.GetString("EntityType"),
                EntityId = _mySqlReader!.GetGuid("EntityId"),
                ProcessedAt = _mySqlReader!.GetDateTime("ProcessedAt")
            };
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(CourseIdempotency record) =>
        throw new NotSupportedException("Use TryRecordAsync(CourseIdempotency, ITransactionContext).");

    protected override StringBuilder UpdateCommandIntoStringBuilder(CourseIdempotency record) =>
        throw new NotSupportedException("CourseIdempotency rows are immutable.");

    public async Task<bool> TryRecordAsync(CourseIdempotency record, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO CourseIdempotency " +
                           "(TenantId, ExternalReference, EntityType, EntityId) " +
                           "VALUES (@tenantId, @externalRef, @entityType, @entityId);";
        MySqlCommand com = new MySqlCommand(sql, _connection, sqlTransaction);
        com.Parameters.AddWithValue("@tenantId", record.TenantId.ToString());
        com.Parameters.AddWithValue("@externalRef", record.ExternalReference);
        com.Parameters.AddWithValue("@entityType", record.EntityType);
        com.Parameters.AddWithValue("@entityId", record.EntityId.ToString());

        try
        {
            await com.ExecuteNonQueryAsync();
            return true;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return false;
        }
    }

    public async Task<CourseIdempotency?> GetByExternalReferenceAsync(Guid tenantId, string externalReference)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT TenantId, ExternalReference, EntityType, EntityId, ProcessedAt " +
                               "FROM CourseIdempotency " +
                               "WHERE TenantId = @tenantId AND ExternalReference = @externalRef;";
            MySqlCommand com = new MySqlCommand(sql, _connection);
            com.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            com.Parameters.AddWithValue("@externalRef", externalReference);

            _mySqlReader = (MySqlDataReader)await com.ExecuteReaderAsync();
            if (!await _mySqlReader.ReadAsync())
            {
                _mySqlReader.Close();
                return (CourseIdempotency?)null;
            }
            CourseIdempotency record = MapReaderToEntity();
            _mySqlReader.Close();
            return (CourseIdempotency?)record;
        });
    }
}
