using Backend.Entities.QrPayments;

using DAMA.Software.MySqlUnitOfWork;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.QrPayments;

public interface IFailedQrPaymentDao : ISingleDao<FailedQrPayment>
{
    Task<bool> TryCreateAsync(FailedQrPayment payment, ITransactionContext transaction);

    Task<int> CountByStudentForTenantAsync(Guid tenantId, Guid studentId);

    Task<List<FailedQrPayment>> GetPageByStudentForTenantAsync(Guid tenantId, Guid studentId, int offset, int limit);
}
