namespace Backend.DB.Daos.Abstract.Single.Todotix;

public interface ITenantTodotixCredentialWriter
{
    Task UpsertAsync(Guid tenantId, string encryptedAppKey);
}
