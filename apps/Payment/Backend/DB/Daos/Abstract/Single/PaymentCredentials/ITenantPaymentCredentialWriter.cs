namespace Backend.DB.Daos.Abstract.Single.PaymentCredentials;

public interface ITenantPaymentCredentialWriter
{
    Task UpsertAsync(Guid tenantId, string todotixAppKey);
}
