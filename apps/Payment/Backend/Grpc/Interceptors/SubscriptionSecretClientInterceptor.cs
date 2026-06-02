using Backend.Options;

using Grpc.Core;
using Grpc.Core.Interceptors;

using Microsoft.Extensions.Options;

namespace Backend.Grpc.Interceptors;

public sealed class SubscriptionSecretClientInterceptor : Interceptor
{
    public const string SecretHeaderName = "x-subscription-secret";

    private readonly SubscriptionGrpcOptions _options;

    public SubscriptionSecretClientInterceptor(IOptions<SubscriptionGrpcOptions> options)
    {
        _options = options.Value;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        Metadata metadata = context.Options.Headers ?? new Metadata();
        metadata.Add(SecretHeaderName, _options.Secret);
        CallOptions callOptions = context.Options.WithHeaders(metadata);
        ClientInterceptorContext<TRequest, TResponse> forwardedContext =
            new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, callOptions);
        return continuation(request, forwardedContext);
    }
}
