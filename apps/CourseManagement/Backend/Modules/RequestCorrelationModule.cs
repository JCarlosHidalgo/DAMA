using Microsoft.Extensions.Primitives;

namespace Backend.Modules;

public sealed class RequestCorrelationModule : IAppModule
{
    private const string CorrelationHeaderName = "X-Correlation-Id";

    public int Order => 12;

    public void Configure(WebApplication app)
    {
        app.Use(async (httpContext, next) =>
        {
            string correlationId =
                httpContext.Request.Headers.TryGetValue(CorrelationHeaderName, out StringValues inbound)
                && !string.IsNullOrWhiteSpace(inbound)
                    ? inbound.ToString()
                    : httpContext.TraceIdentifier;

            httpContext.Response.Headers[CorrelationHeaderName] = correlationId;

            ILoggerFactory loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger("Backend.Request");

            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                await next(httpContext);
            }
        });
    }
}
