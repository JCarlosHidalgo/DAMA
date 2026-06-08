using System.Data;
using System.Text;

using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities.QrPayments;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.QrPayments;

public sealed class FailedQrPaymentDao : MySQLSingleDao<FailedQrPayment>, IFailedQrPaymentDao
{
    public FailedQrPaymentDao(MySqlConnection connection)
    {
        _tableName = "FailedQrPayment";
        _connection = connection;
    }

    protected override FailedQrPayment MapReaderToEntity()
    {
        _entity = new FailedQrPayment
        {
            Id = _mySqlReader!.GetGuid("Id"),
            TenantId = _mySqlReader!.GetGuid("TenantId"),
            StudentId = _mySqlReader!.GetGuid("StudentId"),
            ClassQuantity = _mySqlReader!.GetInt32("ClassQuantity"),
            Cost = _mySqlReader!.GetInt32("Cost"),
            Currency = _mySqlReader!.GetString("Currency"),
            FailedAt = _mySqlReader!.GetDateTime("FailedAt"),
            FailureReason = Enum.Parse<FailureReason>(_mySqlReader!.GetString("FailureReason"))
        };
        return _entity;
    }

    protected override List<FailedQrPayment> MapReaderToEntitiesList()
    {
        _entitiesList = new List<FailedQrPayment>();
        while (_mySqlReader!.Read())
        {
            _entity = new FailedQrPayment
            {
                Id = _mySqlReader!.GetGuid("Id"),
                TenantId = _mySqlReader!.GetGuid("TenantId"),
                StudentId = _mySqlReader!.GetGuid("StudentId"),
                ClassQuantity = _mySqlReader!.GetInt32("ClassQuantity"),
                Cost = _mySqlReader!.GetInt32("Cost"),
                Currency = _mySqlReader!.GetString("Currency"),
                FailedAt = _mySqlReader!.GetDateTime("FailedAt")
            };
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(FailedQrPayment payment) =>
        throw new NotSupportedException("Use TryCreateAsync(FailedQrPayment, ITransactionContext).");

    protected override StringBuilder UpdateCommandIntoStringBuilder(FailedQrPayment payment) =>
        throw new NotSupportedException("FailedQrPayment is immutable.");

    public async Task<bool> TryCreateAsync(FailedQrPayment payment, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO FailedQrPayment (Id, TenantId, StudentId, ClassQuantity, Cost, Currency, FailedAt, FailureReason) " +
                           "VALUES (@Id, @TenantId, @StudentId, @ClassQuantity, @Cost, @Currency, @FailedAt, @FailureReason);";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        insertCommand.Parameters.AddWithValue("@Id", payment.Id);
        insertCommand.Parameters.AddWithValue("@TenantId", payment.TenantId);
        insertCommand.Parameters.AddWithValue("@StudentId", payment.StudentId);
        insertCommand.Parameters.AddWithValue("@ClassQuantity", payment.ClassQuantity);
        insertCommand.Parameters.AddWithValue("@Cost", payment.Cost);
        insertCommand.Parameters.AddWithValue("@Currency", payment.Currency);
        insertCommand.Parameters.AddWithValue("@FailedAt", payment.FailedAt);
        insertCommand.Parameters.AddWithValue("@FailureReason", payment.FailureReason.ToString());

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

    public async Task<int> CountByStudentForTenantAsync(Guid tenantId, Guid studentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT COUNT(*) FROM FailedQrPayment " +
                               "WHERE TenantId = @tenantId AND StudentId = @studentId;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters.AddWithValue("@studentId", studentId.ToString());

            object? countResult = await selectCommand.ExecuteScalarAsync();
            return Convert.ToInt32(countResult ?? 0);
        });
    }

    public async Task<List<FailedQrPayment>> GetPageByStudentForTenantAsync(Guid tenantId, Guid studentId, int offset, int limit)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = GetCommandStoredProcedure("GetFailedQrPaymentsPageByStudentForTenant");
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
}
