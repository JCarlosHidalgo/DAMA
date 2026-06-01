using Backend.Dtos.Todotix.Output;

namespace Backend.Builders;

public interface ITodotixCredentialViewBuilder
{
    TodotixAppKeyStatusDto BuildStatus(bool hasCustomKey, string effectiveAppKey, DateTime? updatedAt);
}
