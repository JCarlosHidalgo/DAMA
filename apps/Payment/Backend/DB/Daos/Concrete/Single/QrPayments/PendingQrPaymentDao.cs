using System.Data;
using System.Text;

using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities.QrPayments;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.QrPayments;

public sealed class PendingQrPaymentDao : MySQLSingleDao<PendingQrPayment>, IPendingQrPaymentDao
{
    public PendingQrPaymentDao(MySqlConnection connection)
    {
        _tableName = "PendingQrPayment";
        _connection = connection;
    }

    protected override PendingQrPayment MapReaderToEntity()
    {
        int qrImageUrlOrdinal = _mySqlReader!.GetOrdinal("QrImageUrl");
        _entity = new PendingQrPayment
        {
            Id = _mySqlReader!.GetGuid("Id"),
            TenantId = _mySqlReader!.GetGuid("TenantId"),
            StudentId = _mySqlReader!.GetGuid("StudentId"),
            TemplateId = _mySqlReader!.GetGuid("TemplateId"),
            ClassQuantity = _mySqlReader!.GetInt32("ClassQuantity"),
            Cost = _mySqlReader!.GetInt32("Cost"),
            QrImageUrl = _mySqlReader!.IsDBNull(qrImageUrlOrdinal) ? null : _mySqlReader!.GetString(qrImageUrlOrdinal),
            CreatedAt = _mySqlReader!.GetDateTime("CreatedAt"),
            ExpiresAt = _mySqlReader!.GetDateTime("ExpiresAt")
        };
        return _entity;
    }

    protected override List<PendingQrPayment> MapReaderToEntitiesList()
    {
        _entitiesList = new List<PendingQrPayment>();
        while (_mySqlReader!.Read())
        {
            int qrImageUrlOrdinal = _mySqlReader!.GetOrdinal("QrImageUrl");
            _entity = new PendingQrPayment
            {
                Id = _mySqlReader!.GetGuid("Id"),
                TenantId = _mySqlReader!.GetGuid("TenantId"),
                StudentId = _mySqlReader!.GetGuid("StudentId"),
                TemplateId = _mySqlReader!.GetGuid("TemplateId"),
                ClassQuantity = _mySqlReader!.GetInt32("ClassQuantity"),
                Cost = _mySqlReader!.GetInt32("Cost"),
                QrImageUrl = _mySqlReader!.IsDBNull(qrImageUrlOrdinal) ? null : _mySqlReader!.GetString(qrImageUrlOrdinal),
                CreatedAt = _mySqlReader!.GetDateTime("CreatedAt"),
                ExpiresAt = _mySqlReader!.GetDateTime("ExpiresAt")
            };
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(PendingQrPayment payment) =>
        throw new NotSupportedException("Use CreateAsync(PendingQrPayment, ITransactionContext).");

    protected override StringBuilder UpdateCommandIntoStringBuilder(PendingQrPayment payment) =>
        throw new NotSupportedException("PendingQrPayment is immutable; delete to transition.");

    public async Task CreateAsync(PendingQrPayment payment, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO PendingQrPayment (Id, TenantId, StudentId, TemplateId, ClassQuantity, Cost, QrImageUrl, ExpiresAt) " +
                           "VALUES (@Id, @TenantId, @StudentId, @TemplateId, @ClassQuantity, @Cost, @QrImageUrl, @ExpiresAt);";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        insertCommand.Parameters.AddWithValue("@Id", payment.Id);
        insertCommand.Parameters.AddWithValue("@TenantId", payment.TenantId);
        insertCommand.Parameters.AddWithValue("@StudentId", payment.StudentId);
        insertCommand.Parameters.AddWithValue("@TemplateId", payment.TemplateId);
        insertCommand.Parameters.AddWithValue("@ClassQuantity", payment.ClassQuantity);
        insertCommand.Parameters.AddWithValue("@Cost", payment.Cost);
        insertCommand.Parameters.AddWithValue("@QrImageUrl", (object?)payment.QrImageUrl ?? DBNull.Value);
        insertCommand.Parameters.AddWithValue("@ExpiresAt", payment.ExpiresAt);

        await insertCommand.ExecuteNonQueryAsync();
    }

    public async Task<List<PendingQrPayment>> GetByStudentForTenantAsync(Guid tenantId, Guid studentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = GetCommandStoredProcedure("GetPendingQrPaymentsByStudentForTenant");
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@studentId", studentId.ToString());
            selectCommand.Parameters["@studentId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<List<PendingQrPayment>> GetByStudentAndTemplateForTenantAsync(Guid tenantId, Guid studentId, Guid templateId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = GetCommandStoredProcedure("GetPendingQrPaymentsByStudentTemplateForTenant");
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@studentId", studentId.ToString());
            selectCommand.Parameters["@studentId"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@templateId", templateId.ToString());
            selectCommand.Parameters["@templateId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<List<PendingQrPayment>> GetPageByStudentForTenantAsync(Guid tenantId, Guid studentId, int offset, int limit)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = GetCommandStoredProcedure("GetPendingQrPaymentsPageByStudentForTenant");
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@studentId", studentId.ToString());
            selectCommand.Parameters["@studentId"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@pageOffset", offset);
            selectCommand.Parameters["@pageOffset"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@pageLimit", limit);
            selectCommand.Parameters["@pageLimit"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            return MapReaderToEntitiesList();
        });
    }

    public async Task<int> CountByStudentForTenantAsync(Guid tenantId, Guid studentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT COUNT(*) FROM PendingQrPayment " +
                               "WHERE TenantId = @tenantId AND StudentId = @studentId;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters.AddWithValue("@studentId", studentId.ToString());

            object? countResult = await selectCommand.ExecuteScalarAsync();
            return Convert.ToInt32(countResult ?? 0);
        });
    }

    public async Task<int> CountActiveForTemplateAsync(Guid tenantId, Guid studentId, Guid templateId, DateTime nowUtc)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT COUNT(*) FROM PendingQrPayment " +
                               "WHERE TenantId = @tenantId AND StudentId = @studentId " +
                               "AND TemplateId = @templateId AND ExpiresAt > @nowUtc;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters.AddWithValue("@studentId", studentId.ToString());
            selectCommand.Parameters.AddWithValue("@templateId", templateId.ToString());
            selectCommand.Parameters.AddWithValue("@nowUtc", nowUtc);

            object? countResult = await selectCommand.ExecuteScalarAsync();
            return Convert.ToInt32(countResult ?? 0);
        });
    }

    public async Task<PendingQrPayment?> GetByIdForTenantAsync(Guid tenantId, Guid paymentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = GetCommandStoredProcedure("GetPendingQrPaymentByIdForTenant");
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@paymentId", paymentId.ToString());
            selectCommand.Parameters["@paymentId"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            if (!await _mySqlReader.ReadAsync())
            {
                _mySqlReader.Close();
                return null;
            }
            PendingQrPayment payment = MapReaderToEntity();
            _mySqlReader.Close();
            return (PendingQrPayment?)payment;
        });
    }

    public async Task<PendingQrPayment?> GetByIdAsync(Guid paymentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT Id, TenantId, StudentId, TemplateId, ClassQuantity, Cost, QrImageUrl, CreatedAt, ExpiresAt " +
                               "FROM PendingQrPayment WHERE Id = @paymentId;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
            selectCommand.Parameters.AddWithValue("@paymentId", paymentId.ToString());

            _mySqlReader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            if (!await _mySqlReader.ReadAsync())
            {
                _mySqlReader.Close();
                return (PendingQrPayment?)null;
            }
            PendingQrPayment payment = MapReaderToEntity();
            _mySqlReader.Close();
            return (PendingQrPayment?)payment;
        });
    }

    public async Task<bool> DeleteForTenantAsync(Guid tenantId, Guid paymentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand deleteCommand = GetCommandStoredProcedure("DeletePendingQrPaymentForTenant");
            deleteCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            deleteCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            deleteCommand.Parameters.AddWithValue("@paymentId", paymentId.ToString());
            deleteCommand.Parameters["@paymentId"].Direction = ParameterDirection.Input;

            object? scalar = await deleteCommand.ExecuteScalarAsync();
            return scalar != null && Convert.ToInt64(scalar) > 0;
        });
    }

    public async Task UpdateQrImageUrlAsync(Guid paymentId, string qrImageUrl)
    {
        await MySQLRetryPolicy.EnsureOpenAsync(_connection);
        const string sql = "UPDATE PendingQrPayment SET QrImageUrl = @qrImageUrl WHERE Id = @paymentId;";
        MySqlCommand updateCommand = new MySqlCommand(sql, _connection);
        updateCommand.Parameters.AddWithValue("@qrImageUrl", qrImageUrl);
        updateCommand.Parameters.AddWithValue("@paymentId", paymentId.ToString());
        await updateCommand.ExecuteNonQueryAsync();
    }

    public async Task<bool> DeleteAsync(Guid paymentId, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "DELETE FROM PendingQrPayment WHERE Id = @paymentId;";
        MySqlCommand deleteCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        deleteCommand.Parameters.AddWithValue("@paymentId", paymentId.ToString());
        int affectedRowCount = await deleteCommand.ExecuteNonQueryAsync();
        return affectedRowCount > 0;
    }
}
