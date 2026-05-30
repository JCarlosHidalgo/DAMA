using Grpc.Core;
using Grpc.Core.Interceptors;

using Microsoft.Net.Http.Headers;

namespace Backend.Grpc.Interceptors;

public sealed class JwtForwardClientInterceptor(IHttpContextAccessor httpContextAccessor) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        string? authHeader = httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Authorization].ToString();
        if (!string.IsNullOrEmpty(authHeader))
        {
            Metadata metadata = context.Options.Headers ?? new Metadata();
            metadata.Add("authorization", authHeader);
            CallOptions options = context.Options.WithHeaders(metadata);
            context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
        }
        return continuation(request, context);
    }
}
