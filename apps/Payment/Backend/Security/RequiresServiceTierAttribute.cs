using Backend.Claims;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Backend.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequiresServiceTierAttribute : Attribute, IAuthorizationFilter
{
    private readonly int _minimumIndex;

    public RequiresServiceTierAttribute(int minimumIndex)
    {
        _minimumIndex = minimumIndex;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        IClaimContext claimContext = context.HttpContext.RequestServices.GetRequiredService<IClaimContext>();
        if (ResolveEffectiveIndex(claimContext) < _minimumIndex)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Insufficient subscription tier.",
                Detail = $"This action requires core-services-pyramid level {_minimumIndex} or higher."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }

    private static int ResolveEffectiveIndex(IClaimContext claimContext)
    {
        try
        {
            return DateTime.UtcNow < claimContext.SubscriptionExpiresAt
                ? claimContext.IndexCoreServicesPyramid
                : 0;
        }
        catch (MissingClaimException)
        {
            return 0;
        }
    }
}
