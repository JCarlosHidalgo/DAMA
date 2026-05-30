using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.Results.Tenants;
using Backend.Services.Concrete.Tenants;

using Moq;

namespace Test.Services.Concrete.Tenants;

[TestFixture]
public class TenantServiceTests
{
    private static readonly Guid CallerTenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private Mock<ITenantDao> tenantDao = null!;
    private Mock<IClaimContext> claimContext = null!;

    private TenantService sut = null!;

    [SetUp]
    public void SetUp()
    {
        tenantDao = new Mock<ITenantDao>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);

        sut = new TenantService(tenantDao.Object, claimContext.Object);
    }

    [Test]
    public async Task UpdateTenantTimezone_WhenCallerBelongsToDifferentTenant_ReturnsForbidden()
    {
        var targetTenantId = Guid.NewGuid();
        claimContext.Setup(accessor => accessor.TenantId).Returns(CallerTenantId);

        UpdateTenantTimezoneOutcome outcome =
            await sut.UpdateTenantTimezone(targetTenantId, "America/La_Paz");

        Assert.That(outcome, Is.InstanceOf<UpdateTenantTimezoneOutcome.Forbidden>());
        tenantDao.Verify(
            dao => dao.UpdateTimezoneAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task UpdateTenantTimezone_WhenTenantNotFound_ReturnsNotFound()
    {
        claimContext.Setup(accessor => accessor.TenantId).Returns(CallerTenantId);
        tenantDao
            .Setup(dao => dao.UpdateTimezoneAsync(CallerTenantId, "America/Bogota"))
            .ReturnsAsync(0);

        UpdateTenantTimezoneOutcome outcome =
            await sut.UpdateTenantTimezone(CallerTenantId, "America/Bogota");

        Assert.That(outcome, Is.InstanceOf<UpdateTenantTimezoneOutcome.NotFound>());
    }

    [Test]
    public async Task UpdateTenantTimezone_WhenUpdateAffectsRows_ReturnsUpdated()
    {
        claimContext.Setup(accessor => accessor.TenantId).Returns(CallerTenantId);
        tenantDao
            .Setup(dao => dao.UpdateTimezoneAsync(CallerTenantId, "Europe/Madrid"))
            .ReturnsAsync(1);

        UpdateTenantTimezoneOutcome outcome =
            await sut.UpdateTenantTimezone(CallerTenantId, "Europe/Madrid");

        Assert.That(outcome, Is.InstanceOf<UpdateTenantTimezoneOutcome.Updated>());
    }
}
