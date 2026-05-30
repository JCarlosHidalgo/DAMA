using Backend.DB.Daos.Abstract.Single.Events;

using DAMA.Software.MySqlUnitOfWork;

using MySql.Data.MySqlClient;

namespace Backend.DB.Daos.Concrete.Single.Events;

public sealed class PaymentCreditLedgerDao : IPaymentCreditLedgerDao
{
    private readonly MySqlConnection _connection;

    public PaymentCreditLedgerDao(MySqlConnection connection)
    {
        _connection = connection;
    }

    public async Task RecordAsync(Guid eventId, Guid tenantId, Guid studentId, int quantity, string externalReference, DateTime occurredAt, ITransactionContext transaction)
    {
        MySqlTransaction sqlTransaction = MySqlTransactionContextAccessor.Unwrap(transaction);
        const string sql = "INSERT INTO payment_credit_ledger " +
                           "(EventId, TenantId, StudentId, Quantity, ExternalReference, OccurredAt, CreatedAt) " +
                           "VALUES (@eventId, @tenantId, @studentId, @quantity, @externalReference, @occurredAt, NOW(6));";
        MySqlCommand command = new MySqlCommand(sql, _connection, sqlTransaction);
        command.Parameters.AddWithValue("@eventId", eventId.ToString());
        command.Parameters.AddWithValue("@tenantId", tenantId.ToString());
        command.Parameters.AddWithValue("@studentId", studentId.ToString());
        command.Parameters.AddWithValue("@quantity", quantity);
        command.Parameters.AddWithValue("@externalReference", externalReference);
        command.Parameters.AddWithValue("@occurredAt", occurredAt);
        await command.ExecuteNonQueryAsync();
    }
}
