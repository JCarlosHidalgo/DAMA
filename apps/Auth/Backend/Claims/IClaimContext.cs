namespace Backend.Claims;

public interface IClaimContext
{
    Guid TenantId { get; }

    string TenantName { get; }

    string TenantTimezone { get; }

    Guid UserId { get; }

    string UserName { get; }

    string Role { get; }

    int IndexCoreServicesPyramid { get; }

    DateTime SubscriptionExpiresAt { get; }
}
