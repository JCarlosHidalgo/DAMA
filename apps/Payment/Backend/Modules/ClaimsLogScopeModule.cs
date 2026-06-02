using Backend.Security;

namespace Backend.Modules;

public sealed class ClaimsLogScopeModule : IAppModule
{
    public int Order => 35;

    public void Configure(WebApplication app)
    {
        app.Use(async (httpContext, next) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                await next(httpContext);
                return;
            }

            Dictionary<string, object> scopeState = new Dictionary<string, object>();
            AddClaim(scopeState, "TenantId", httpContext.User.FindFirst(AuthClaims.TenantId)?.Value);
            AddClaim(scopeState, "UserId", httpContext.User.FindFirst(AuthClaims.UserId)?.Value);
            AddClaim(scopeState, "Role", httpContext.User.FindFirst(AuthClaims.Role)?.Value);

            if (scopeState.Count == 0)
            {
                await next(httpContext);
                return;
            }

            ILoggerFactory loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger("Backend.Request");

            using (logger.BeginScope(scopeState))
            {
                await next(httpContext);
            }
        });
    }

    private static void AddClaim(Dictionary<string, object> scopeState, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            scopeState[key] = value;
        }
    }
}
