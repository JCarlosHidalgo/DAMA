using Grpc.Net.Client;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Backend.ExternalCheck;

public sealed class GrpcPeerHealthCheck : IHealthCheck
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);

    private readonly string _peerAddress;

    public GrpcPeerHealthCheck(string peerAddress)
    {
        _peerAddress = peerAddress;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        GrpcChannel? channel = null;
        try
        {
            using CancellationTokenSource timeoutSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(ConnectTimeout);

            channel = GrpcChannel.ForAddress(_peerAddress);
            await channel.ConnectAsync(timeoutSource.Token);

            return HealthCheckResult.Healthy($"gRPC peer {_peerAddress} is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy($"gRPC peer {_peerAddress} is unreachable.", exception);
        }
        finally
        {
            channel?.Dispose();
        }
    }
}
