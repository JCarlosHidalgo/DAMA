using System.Security.Cryptography;
using System.Text;

using Backend.Logging;
using Backend.Options;
using Backend.Services.Abstract.Subscriptions;

using DAMA.Software.GrpcContracts;

using Grpc.Core;

using Microsoft.Extensions.Options;

namespace Backend.Grpc.Services;

public sealed class TenantSubscriptionGrpcService : TenantSubscription.TenantSubscriptionBase
{
    public const string SecretHeaderName = "x-subscription-secret";

    private readonly ITenantSubscriptionUpdater _subscriptionUpdater;
    private readonly SubscriptionGrpcOptions _options;
    private readonly ILogger<TenantSubscriptionGrpcService> _logger;

    public TenantSubscriptionGrpcService(ITenantSubscriptionUpdater subscriptionUpdater,
                                         IOptions<SubscriptionGrpcOptions> options,
                                         ILogger<TenantSubscriptionGrpcService> logger)
    {
        _subscriptionUpdater = subscriptionUpdater;
        _options = options.Value;
        _logger = logger;
    }

    public override async Task<UpdateTenantSubscriptionResponse> UpdateTenantSubscription(
        UpdateTenantSubscriptionRequest request, ServerCallContext context)
    {
        if (!IsAuthorized(context))
        {
            LogEvents.TenantSubscriptionUnauthorized(_logger);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid service secret."));
        }

        if (!Guid.TryParse(request.TenantId, out Guid tenantId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "tenant_id is not a valid GUID."));
        }

        DateTime newExpiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(request.NewExpiresAtUnix).UtcDateTime;
        await _subscriptionUpdater.UpdateAsync(tenantId, request.Level, newExpiresAtUtc);

        LogEvents.TenantSubscriptionUpdated(_logger, tenantId, request.Level);
        return new UpdateTenantSubscriptionResponse { Updated = true };
    }

    private bool IsAuthorized(ServerCallContext context)
    {
        string? presentedSecret = context.RequestHeaders.GetValue(SecretHeaderName);
        if (string.IsNullOrEmpty(presentedSecret) || string.IsNullOrEmpty(_options.Secret))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(presentedSecret),
            Encoding.UTF8.GetBytes(_options.Secret));
    }
}
