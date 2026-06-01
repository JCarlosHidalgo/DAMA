using Backend.DB.Daos.Abstract.Single.PaymentCredentials;
using Backend.Entities.PaymentCredentials;
using Backend.Security;
using Backend.Services.Abstract.Todotix;

namespace Backend.Services.Concrete.Todotix;

public sealed class TodotixAppKeyResolver(
    ITenantPaymentCredentialReader credentialReader,
    IAppKeyCipher appKeyCipher) : ITodotixAppKeyResolver
{
    public async Task<string?> ResolveAsync(Guid tenantId)
    {
        TenantPaymentCredential? credential = await credentialReader.GetByTenantAsync(tenantId);
        return credential is null
            ? null
            : appKeyCipher.Decrypt(credential.TodotixAppKey);
    }
}
