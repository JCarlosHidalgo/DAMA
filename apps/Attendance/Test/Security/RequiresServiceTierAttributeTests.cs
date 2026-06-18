using Backend.Claims;
using Backend.Security;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace Test.Security;

[TestFixture]
public class RequiresServiceTierAttributeTests
{
    private static AuthorizationFilterContext BuildContext(IClaimContext claimContext)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(claimContext);
        httpContext.RequestServices = services.BuildServiceProvider();

        ActionContext actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static Mock<IClaimContext> ClaimContextWith(int index, DateTime expiresAt)
    {
        Mock<IClaimContext> claimContext = new Mock<IClaimContext>(MockBehavior.Loose);
        claimContext.Setup(context => context.IndexCoreServicesPyramid).Returns(index);
        claimContext.Setup(context => context.SubscriptionExpiresAt).Returns(expiresAt);
        return claimContext;
    }

    [Test]
    public void OnAuthorization_WhenEffectiveIndexMeetsMinimum_AllowsRequest()
    {
        Mock<IClaimContext> claimContext = ClaimContextWith(3, DateTime.UtcNow.AddDays(10));
        AuthorizationFilterContext context = BuildContext(claimContext.Object);

        new RequiresServiceTierAttribute(3).OnAuthorization(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public void OnAuthorization_WhenEffectiveIndexBelowMinimum_Returns403()
    {
        Mock<IClaimContext> claimContext = ClaimContextWith(2, DateTime.UtcNow.AddDays(10));
        AuthorizationFilterContext context = BuildContext(claimContext.Object);

        new RequiresServiceTierAttribute(3).OnAuthorization(context);

        ObjectResult? result = context.Result as ObjectResult;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public void OnAuthorization_WhenSubscriptionExpired_TreatsIndexAsZeroAndDenies()
    {
        Mock<IClaimContext> claimContext = ClaimContextWith(3, DateTime.UtcNow.AddMinutes(-1));
        AuthorizationFilterContext context = BuildContext(claimContext.Object);

        new RequiresServiceTierAttribute(1).OnAuthorization(context);

        ObjectResult? result = context.Result as ObjectResult;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }

    [Test]
    public void OnAuthorization_WhenClaimMissing_TreatsIndexAsZeroAndDenies()
    {
        Mock<IClaimContext> claimContext = new Mock<IClaimContext>(MockBehavior.Loose);
        claimContext.Setup(context => context.SubscriptionExpiresAt).Throws(new MissingClaimException("subscription_expires_at"));
        AuthorizationFilterContext context = BuildContext(claimContext.Object);

        new RequiresServiceTierAttribute(1).OnAuthorization(context);

        ObjectResult? result = context.Result as ObjectResult;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
    }
}
