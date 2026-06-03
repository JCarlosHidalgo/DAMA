using Backend.Entities.QrPayments;

using DAMA.Software.MySqlUnitOfWork;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.QrPayments;

public interface IPendingQrPaymentDao : ISingleDao<PendingQrPayment>
{
    Task CreateAsync(PendingQrPayment payment, ITransactionContext transaction);

    Task<List<PendingQrPayment>> GetByStudentForTenantAsync(Guid tenantId, Guid studentId);

    Task<List<PendingQrPayment>> GetByStudentAndTemplateForTenantAsync(Guid tenantId, Guid studentId, Guid templateId);

    Task<List<PendingQrPayment>> GetPageByStudentForTenantAsync(Guid tenantId, Guid studentId, int offset, int limit);

    Task<int> CountByStudentForTenantAsync(Guid tenantId, Guid studentId);

    Task<Guid?> GetActiveForTemplateAsync(Guid tenantId, Guid studentId, Guid templateId, DateTime nowUtc);

    Task<PendingQrPayment?> GetByIdForTenantAsync(Guid tenantId, Guid paymentId);

    Task<PendingQrPayment?> GetByIdAsync(Guid paymentId);

    Task<bool> DeleteForTenantAsync(Guid tenantId, Guid paymentId);

    Task UpdateQrImageUrlAsync(Guid paymentId, string qrImageUrl);

    Task<bool> DeleteAsync(Guid paymentId, ITransactionContext transaction);
}
