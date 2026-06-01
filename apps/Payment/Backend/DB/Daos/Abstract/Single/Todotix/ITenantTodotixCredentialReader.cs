using Backend.Entities.Todotix;

namespace Backend.DB.Daos.Abstract.Single.Todotix;

public interface ITenantTodotixCredentialReader
{
    Task<TenantTodotixCredential?> GetByTenantAsync(Guid tenantId);
}
