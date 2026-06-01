namespace Backend.Services.Abstract.Todotix;

public interface ITodotixAppKeyResolver
{
    Task<string?> ResolveAsync(Guid tenantId);
}
