using System.Security.Claims;

using Backend.Claims;

namespace Test.Infrastructure.Claims;

[TestFixture]
public class HubClaimContextTests
{
    private static readonly Guid SampleTenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private HubClaimContext _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new HubClaimContext();

    private static ClaimsPrincipal PrincipalWith(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }

    [Test]
    public void GetTenantId_WhenClaimPresentAndValid_ReturnsParsedGuid()
    {
        ClaimsPrincipal user = PrincipalWith(new Claim("tenant_id", SampleTenantId.ToString()));

        Assert.That(_sut.GetTenantId(user), Is.EqualTo(SampleTenantId));
    }

    [Test]
    public void GetTenantId_WhenClaimMissing_ThrowsMissingClaim()
    {
        ClaimsPrincipal user = PrincipalWith(new Claim("user_id", Guid.NewGuid().ToString()));

        MissingClaimException exception = Assert.Throws<MissingClaimException>(() => _sut.GetTenantId(user));
        Assert.That(exception.ClaimName, Is.EqualTo("tenant_id"));
    }

    [Test]
    public void GetTenantId_WhenClaimMalformed_ThrowsMissingClaim()
    {
        ClaimsPrincipal user = PrincipalWith(new Claim("tenant_id", "not-a-guid"));

        Assert.Throws<MissingClaimException>(() => _sut.GetTenantId(user));
    }

    [Test]
    public void GetTenantId_WhenPrincipalNull_ThrowsMissingClaim()
    {
        Assert.Throws<MissingClaimException>(() => _sut.GetTenantId(null));
    }
}
