using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Entities.Todotix;
using Backend.Options;
using Backend.Security;
using Backend.Services.Abstract.Todotix;

using Microsoft.Extensions.Options;

namespace Backend.Services.Concrete.Todotix;

public sealed class TodotixAppKeyResolver(
    ITenantTodotixCredentialReader credentialReader,
    IAppKeyCipher appKeyCipher,
    IOptions<TodotixOptions> todotixOptions) : ITodotixAppKeyResolver
{
    public async Task<string> ResolveAsync(Guid tenantId)
    {
        TenantTodotixCredential? credential = await credentialReader.GetByTenantAsync(tenantId);
        return credential is null
            ? todotixOptions.Value.ApplicationKey
            : appKeyCipher.Decrypt(credential.EncryptedAppKey);
    }
}
