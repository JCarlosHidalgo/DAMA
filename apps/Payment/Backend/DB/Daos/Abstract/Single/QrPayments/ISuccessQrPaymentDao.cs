using Backend.Entities.QrPayments;

using DAMA.Software.MySqlUnitOfWork;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.QrPayments;

public interface ISuccessQrPaymentDao : ISingleDao<SuccessQrPayment>
{
    Task<bool> TryCreateAsync(SuccessQrPayment payment, ITransactionContext transaction);

    Task<SuccessQrPayment?> GetByIdAsync(Guid paymentId);

    Task<int> CountByStudentForTenantAsync(Guid tenantId, Guid studentId);

    Task<List<SuccessQrPayment>> GetPageByStudentForTenantAsync(Guid tenantId, Guid studentId, int offset, int limit);

    Task<(int total, int windowTotal, DateTime? firstPaymentDate)> GetSummaryAsync(Guid tenantId, DateTime fromDate);
}
