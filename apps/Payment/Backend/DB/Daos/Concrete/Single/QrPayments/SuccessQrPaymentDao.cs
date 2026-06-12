using System.Data;
using System.Text;

using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Entities.QrPayments;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

using SQLDaosPackage.Daos.MySQL;

namespace Backend.DB.Daos.Concrete.Single.QrPayments;

public sealed class SuccessQrPaymentDao : MySQLSingleDao<SuccessQrPayment>, ISuccessQrPaymentDao
{
    public SuccessQrPaymentDao(MySqlConnection connection)
    {
        _tableName = "SuccessQrPayment";
        _connection = connection;
    }

    protected override SuccessQrPayment MapReaderToEntity()
    {
        _entity = new SuccessQrPayment
        {
            Id = _mySqlReader!.GetGuid("Id"),
            TenantId = _mySqlReader!.GetGuid("TenantId"),
            StudentId = _mySqlReader!.GetGuid("StudentId"),
            ClassQuantity = _mySqlReader!.GetInt32("ClassQuantity"),
            Cost = _mySqlReader!.GetInt32("Cost"),
            Currency = _mySqlReader!.GetString("Currency"),
            PaidAt = _mySqlReader!.GetDateTime("PaidAt")
        };
        return _entity;
    }

    protected override List<SuccessQrPayment> MapReaderToEntitiesList()
    {
        _entitiesList = new List<SuccessQrPayment>();
        while (_mySqlReader!.Read())
        {
            _entity = new SuccessQrPayment
            {
                Id = _mySqlReader!.GetGuid("Id"),
                TenantId = _mySqlReader!.GetGuid("TenantId"),
                StudentId = _mySqlReader!.GetGuid("StudentId"),
                ClassQuantity = _mySqlReader!.GetInt32("ClassQuantity"),
                Cost = _mySqlReader!.GetInt32("Cost"),
                Currency = _mySqlReader!.GetString("Currency"),
                PaidAt = _mySqlReader!.GetDateTime("PaidAt")
            };
            _entitiesList.Add(_entity);
        }
        _mySqlReader.Close();
        return _entitiesList;
    }

    protected override StringBuilder CreateCommandIntoStringBuilder(SuccessQrPayment payment) =>
        throw new NotSupportedException("Use TryCreateAsync(SuccessQrPayment, ITransactionContext).");

    protected override StringBuilder UpdateCommandIntoStringBuilder(SuccessQrPayment payment) =>
        throw new NotSupportedException("SuccessQrPayment is immutable.");

    public async Task<bool> TryCreateAsync(SuccessQrPayment payment, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO SuccessQrPayment (Id, TenantId, StudentId, ClassQuantity, Cost, Currency, PaidAt) " +
                           "VALUES (@Id, @TenantId, @StudentId, @ClassQuantity, @Cost, @Currency, @PaidAt);";
        MySqlCommand insertCommand = new MySqlCommand(sql, _connection, sqlTransaction);
        insertCommand.Parameters.AddWithValue("@Id", payment.Id);
        insertCommand.Parameters.AddWithValue("@TenantId", payment.TenantId);
        insertCommand.Parameters.AddWithValue("@StudentId", payment.StudentId);
        insertCommand.Parameters.AddWithValue("@ClassQuantity", payment.ClassQuantity);
        insertCommand.Parameters.AddWithValue("@Cost", payment.Cost);
        insertCommand.Parameters.AddWithValue("@Currency", payment.Currency);
        insertCommand.Parameters.AddWithValue("@PaidAt", payment.PaidAt);

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

    public async Task<(int total, int windowTotal, DateTime? firstPaymentDate)> GetSummaryAsync(Guid tenantId, DateTime fromDate)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = GetCommandStoredProcedure("GetPaymentSummaryForTenant");
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters["@tenantId"].Direction = ParameterDirection.Input;
            selectCommand.Parameters.AddWithValue("@fromDate", fromDate);
            selectCommand.Parameters["@fromDate"].Direction = ParameterDirection.Input;

            _mySqlReader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            int totalEarnings = 0;
            int monthEarnings = 0;
            DateTime? firstPaymentDate = null;
            if (await _mySqlReader.ReadAsync())
            {
                totalEarnings = _mySqlReader.GetInt32("TotalEarnings");
                monthEarnings = _mySqlReader.GetInt32("MonthEarnings");
                firstPaymentDate = _mySqlReader.IsDBNull(_mySqlReader.GetOrdinal("FirstPaymentDate"))
                                       ? (DateTime?)null
                                       : _mySqlReader.GetDateTime("FirstPaymentDate");
            }
            _mySqlReader.Close();
            return (totalEarnings, monthEarnings, firstPaymentDate);
        });
    }

    public async Task<int> CountByStudentForTenantAsync(Guid tenantId, Guid studentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT COUNT(*) FROM SuccessQrPayment " +
                               "WHERE TenantId = @tenantId AND StudentId = @studentId;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());
            selectCommand.Parameters.AddWithValue("@studentId", studentId.ToString());

            object? countResult = await selectCommand.ExecuteScalarAsync();
            return Convert.ToInt32(countResult ?? 0);
        });
    }

    public async Task<List<SuccessQrPayment>> GetPageByStudentForTenantAsync(Guid tenantId, Guid studentId, int offset, int limit)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            MySqlCommand selectCommand = GetCommandStoredProcedure("GetSuccessQrPaymentsPageByStudentForTenant");
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

    public async Task<SuccessQrPayment?> GetByIdForTenantAsync(Guid tenantId, Guid paymentId)
    {
        return await MySQLRetryPolicy.ExecuteAsync(_connection, async () =>
        {
            const string sql = "SELECT Id, TenantId, StudentId, ClassQuantity, Cost, Currency, PaidAt " +
                               "FROM SuccessQrPayment WHERE Id = @paymentId AND TenantId = @tenantId;";
            MySqlCommand selectCommand = new MySqlCommand(sql, _connection);
            selectCommand.Parameters.AddWithValue("@paymentId", paymentId.ToString());
            selectCommand.Parameters.AddWithValue("@tenantId", tenantId.ToString());

            _mySqlReader = (MySqlDataReader)await selectCommand.ExecuteReaderAsync();
            if (!await _mySqlReader.ReadAsync())
            {
                _mySqlReader.Close();
                return (SuccessQrPayment?)null;
            }
            SuccessQrPayment payment = MapReaderToEntity();
            _mySqlReader.Close();
            return (SuccessQrPayment?)payment;
        });
    }
}
