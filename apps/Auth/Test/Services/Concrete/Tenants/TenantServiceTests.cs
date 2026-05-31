using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.Dtos.Tenants.Input;
using Backend.Dtos.Tenants.Output;
using Backend.Entities.Tenants;
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
    private Mock<ITenantBuilder> tenantBuilder = null!;

    private TenantService sut = null!;

    [SetUp]
    public void SetUp()
    {
        tenantDao = new Mock<ITenantDao>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        tenantBuilder = new Mock<ITenantBuilder>(MockBehavior.Strict);

        sut = new TenantService(tenantDao.Object, claimContext.Object, tenantBuilder.Object);
    }

    [Test]
    public async Task GetAllTenants_ReturnsTenantsProjectedByBuilder()
    {
        List<Tenant> tenants = new()
        {
            new Tenant { Id = Guid.NewGuid(), Name = "Escuela Example", Timezone = "America/La_Paz" }
        };
        List<TenantDto> projected = new()
        {
            new TenantDto { Id = tenants[0].Id, Name = "Escuela Example", Timezone = "America/La_Paz" }
        };

        tenantDao.Setup(dao => dao.ReadAllAsync()).ReturnsAsync(tenants);
        tenantBuilder.Setup(builder => builder.BuildTenantDtos(tenants)).Returns(projected);

        List<TenantDto> result = await sut.GetAllTenants();

        Assert.That(result, Is.SameAs(projected));
    }

    [Test]
    public async Task CreateTenant_BuildsTenantPersistsItAndReturnsDto()
    {
        Tenant built = new() { Id = Guid.NewGuid(), Name = "Nueva Escuela", Timezone = "America/La_Paz" };
        TenantDto dto = new() { Id = built.Id, Name = built.Name, Timezone = built.Timezone };

        tenantBuilder.Setup(builder => builder.BuildTenant("Nueva Escuela")).Returns(built);
        tenantDao.Setup(dao => dao.CreateTenantAsync(built)).Returns(Task.CompletedTask);
        tenantBuilder.Setup(builder => builder.BuildTenantDto(built)).Returns(dto);

        TenantDto result = await sut.CreateTenant(new CreateTenantDto { Name = "Nueva Escuela" });

        Assert.That(result, Is.SameAs(dto));
        tenantDao.Verify(dao => dao.CreateTenantAsync(built), Times.Once);
    }

    [Test]
    public async Task RenameTenant_WhenUpdateAffectsRows_ReturnsUpdated()
    {
        var targetTenantId = Guid.NewGuid();
        tenantDao.Setup(dao => dao.UpdateNameAsync(targetTenantId, "Renombrado")).ReturnsAsync(1);

        UpdateTenantNameOutcome outcome = await sut.RenameTenant(targetTenantId, "Renombrado");

        Assert.That(outcome, Is.InstanceOf<UpdateTenantNameOutcome.Updated>());
    }

    [Test]
    public async Task RenameTenant_WhenTenantNotFound_ReturnsNotFound()
    {
        var targetTenantId = Guid.NewGuid();
        tenantDao.Setup(dao => dao.UpdateNameAsync(targetTenantId, "Renombrado")).ReturnsAsync(0);

        UpdateTenantNameOutcome outcome = await sut.RenameTenant(targetTenantId, "Renombrado");

        Assert.That(outcome, Is.InstanceOf<UpdateTenantNameOutcome.NotFound>());
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
