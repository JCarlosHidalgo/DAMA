using Backend.Dtos.External.Todotix;

namespace Backend.Builders;

public interface ITodotixCredentialTestBuilder
{
    RegisterDebtRequest BuildCredentialTestRequest(string appKey, string tenantTimezone);
}
