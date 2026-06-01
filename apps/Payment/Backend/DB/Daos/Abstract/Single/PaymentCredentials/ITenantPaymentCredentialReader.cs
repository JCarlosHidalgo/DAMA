using Backend.Entities.PaymentCredentials;

namespace Backend.DB.Daos.Abstract.Single.PaymentCredentials;

public interface ITenantPaymentCredentialReader
{
    Task<TenantPaymentCredential?> GetByTenantAsync(Guid tenantId);
}
