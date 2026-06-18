using System.Security.Claims;

using Backend.Claims;

using Microsoft.AspNetCore.Http;

using Moq;

namespace Test.Infrastructure.Claims;

[TestFixture]
public class ClaimContextTests
{
    private static readonly Guid SampleTenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SampleUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private const string SampleTenantName = "Sample Tenant";
    private const string SampleTenantTimezone = "America/Lima";
    private const string SampleUserName = "alice";
    private const string SampleRole = "Admin";

    private Mock<IHttpContextAccessor> _httpContextAccessor = null!;

    [SetUp]
    public void SetUp() => _httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);

    private void GivenClaims(params Claim[] claims)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessor.SetupGet(accessor => accessor.HttpContext).Returns(httpContext);
    }

    [Test]
    public void TenantId_WhenClaimPresentAndValid_ReturnsParsedGuid()
    {
        GivenClaims(new Claim("tenant_id", SampleTenantId.ToString()));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.That(claimContext.TenantId, Is.EqualTo(SampleTenantId));
    }

    [Test]
    public void TenantId_CachesValueAcrossMultipleReads()
    {
        GivenClaims(new Claim("tenant_id", SampleTenantId.ToString()));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Guid first = claimContext.TenantId;
        Guid second = claimContext.TenantId;

        Assert.That(first, Is.EqualTo(second));
        _httpContextAccessor.Verify(accessor => accessor.HttpContext, Times.Once);
    }

    [Test]
    public void TenantId_WhenHttpContextIsNull_ThrowsMissingClaimException()
    {
        _httpContextAccessor.SetupGet(accessor => accessor.HttpContext).Returns((HttpContext?)null);
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        MissingClaimException exception = Assert.Throws<MissingClaimException>(() => _ = claimContext.TenantId)!;
        Assert.That(exception.ClaimName, Is.EqualTo("tenant_id"));
    }

    [Test]
    public void TenantId_WhenClaimMissing_ThrowsMissingClaimException()
    {
        GivenClaims();
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.TenantId);
    }

    [Test]
    public void TenantId_WhenClaimMalformed_ThrowsMissingClaimException()
    {
        GivenClaims(new Claim("tenant_id", "not-a-guid"));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.TenantId);
    }

    [Test]
    public void UserId_WhenClaimPresentAndValid_ReturnsParsedGuid()
    {
        GivenClaims(new Claim("user_id", SampleUserId.ToString()));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.That(claimContext.UserId, Is.EqualTo(SampleUserId));
    }

    [Test]
    public void UserId_CachesValueAcrossMultipleReads()
    {
        GivenClaims(new Claim("user_id", SampleUserId.ToString()));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Guid first = claimContext.UserId;
        Guid second = claimContext.UserId;

        Assert.That(first, Is.EqualTo(second));
        _httpContextAccessor.Verify(accessor => accessor.HttpContext, Times.Once);
    }

    [Test]
    public void UserId_WhenClaimMissing_ThrowsMissingClaimException()
    {
        GivenClaims();
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.UserId);
    }

    [Test]
    public void UserId_WhenClaimMalformed_ThrowsMissingClaimException()
    {
        GivenClaims(new Claim("user_id", "  "));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.UserId);
    }

    [Test]
    public void TenantName_WhenClaimPresent_ReturnsValue()
    {
        GivenClaims(new Claim("tenant_name", SampleTenantName));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.That(claimContext.TenantName, Is.EqualTo(SampleTenantName));
    }

    [Test]
    public void TenantName_CachesValueAcrossMultipleReads()
    {
        GivenClaims(new Claim("tenant_name", SampleTenantName));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        string first = claimContext.TenantName;
        string second = claimContext.TenantName;

        Assert.That(first, Is.EqualTo(second));
        _httpContextAccessor.Verify(accessor => accessor.HttpContext, Times.Once);
    }

    [Test]
    public void TenantName_WhenClaimMissing_ThrowsMissingClaimException()
    {
        GivenClaims();
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.TenantName);
    }

    [Test]
    public void TenantName_WhenClaimWhitespace_ThrowsMissingClaimException()
    {
        GivenClaims(new Claim("tenant_name", "   "));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.TenantName);
    }

    [Test]
    public void TenantTimezone_WhenClaimPresent_ReturnsValue()
    {
        GivenClaims(new Claim("tenant_timezone", SampleTenantTimezone));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.That(claimContext.TenantTimezone, Is.EqualTo(SampleTenantTimezone));
    }

    [Test]
    public void TenantTimezone_WhenClaimMissing_ThrowsMissingClaimException()
    {
        GivenClaims();
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.TenantTimezone);
    }

    [Test]
    public void UserName_WhenClaimPresent_ReturnsValue()
    {
        GivenClaims(new Claim("user_name", SampleUserName));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.That(claimContext.UserName, Is.EqualTo(SampleUserName));
    }

    [Test]
    public void UserName_WhenClaimMissing_ThrowsMissingClaimException()
    {
        GivenClaims();
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.UserName);
    }

    [Test]
    public void Role_WhenClaimPresent_ReturnsValue()
    {
        GivenClaims(new Claim("role", SampleRole));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.That(claimContext.Role, Is.EqualTo(SampleRole));
    }

    [Test]
    public void Role_WhenClaimMissing_ThrowsMissingClaimException()
    {
        GivenClaims();
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.Role);
    }

    [Test]
    public void IndexCoreServicesPyramid_WhenClaimPresentAndValid_ReturnsParsedInt()
    {
        GivenClaims(new Claim("index_core_services_pyramid", "3"));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.That(claimContext.IndexCoreServicesPyramid, Is.EqualTo(3));
    }

    [Test]
    public void IndexCoreServicesPyramid_CachesValueAcrossMultipleReads()
    {
        GivenClaims(new Claim("index_core_services_pyramid", "3"));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        int first = claimContext.IndexCoreServicesPyramid;
        int second = claimContext.IndexCoreServicesPyramid;

        Assert.That(first, Is.EqualTo(second));
        _httpContextAccessor.Verify(accessor => accessor.HttpContext, Times.Once);
    }

    [Test]
    public void IndexCoreServicesPyramid_WhenClaimMissing_ThrowsMissingClaimException()
    {
        GivenClaims();
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.IndexCoreServicesPyramid);
    }

    [Test]
    public void IndexCoreServicesPyramid_WhenClaimMalformed_ThrowsMissingClaimException()
    {
        GivenClaims(new Claim("index_core_services_pyramid", "not-a-number"));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.IndexCoreServicesPyramid);
    }

    [Test]
    public void SubscriptionExpiresAt_WhenClaimPresentAndValid_ReturnsParsedUtcDateTime()
    {
        GivenClaims(new Claim("subscription_expires_at", "1700000000"));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        DateTime expected = DateTimeOffset.FromUnixTimeSeconds(1700000000).UtcDateTime;
        Assert.That(claimContext.SubscriptionExpiresAt, Is.EqualTo(expected));
    }

    [Test]
    public void SubscriptionExpiresAt_CachesValueAcrossMultipleReads()
    {
        GivenClaims(new Claim("subscription_expires_at", "1700000000"));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        DateTime first = claimContext.SubscriptionExpiresAt;
        DateTime second = claimContext.SubscriptionExpiresAt;

        Assert.That(first, Is.EqualTo(second));
        _httpContextAccessor.Verify(accessor => accessor.HttpContext, Times.Once);
    }

    [Test]
    public void SubscriptionExpiresAt_WhenClaimMissing_ThrowsMissingClaimException()
    {
        GivenClaims();
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.SubscriptionExpiresAt);
    }

    [Test]
    public void SubscriptionExpiresAt_WhenClaimMalformed_ThrowsMissingClaimException()
    {
        GivenClaims(new Claim("subscription_expires_at", "not-a-number"));
        var claimContext = new ClaimContext(_httpContextAccessor.Object);

        Assert.Throws<MissingClaimException>(() => _ = claimContext.SubscriptionExpiresAt);
    }
}
