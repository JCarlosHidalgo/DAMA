using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Tenants;
using Backend.Dtos.Tenants.Input;
using Backend.Dtos.Tenants.Output;
using Backend.Entities.Tenants;
using Backend.Results.Tenants;
using Backend.Services.Concrete.Tenants;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Test.Services.Concrete.Tenants;

[TestFixture]
public class TenantServiceTests
{
    private static readonly Guid CallerTenantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CallerUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private Mock<ITenantDao> _tenantDao = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<ITenantBuilder> _tenantBuilder = null!;

    private TenantService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _tenantDao = new Mock<ITenantDao>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _tenantBuilder = new Mock<ITenantBuilder>(MockBehavior.Strict);

        _claimContext.Setup(accessor => accessor.UserId).Returns(CallerUserId);

        _sut = new TenantService(
            _tenantDao.Object,
            _claimContext.Object,
            _tenantBuilder.Object,
            NullLogger<TenantService>.Instance);
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

        _tenantDao.Setup(dao => dao.ReadAllAsync()).ReturnsAsync(tenants);
        _tenantBuilder.Setup(builder => builder.BuildTenantDtos(tenants)).Returns(projected);

        List<TenantDto> result = await _sut.GetAllTenants();

        Assert.That(result, Is.SameAs(projected));
    }

    [Test]
    public async Task CreateTenant_BuildsTenantPersistsItAndReturnsDto()
    {
        Tenant built = new() { Id = Guid.NewGuid(), Name = "Nueva Escuela", Timezone = "America/La_Paz" };
        TenantDto dto = new() { Id = built.Id, Name = built.Name, Timezone = built.Timezone };

        _tenantBuilder.Setup(builder => builder.BuildTenant("Nueva Escuela")).Returns(built);
        _tenantDao.Setup(dao => dao.CreateTenantAsync(built)).Returns(Task.CompletedTask);
        _tenantBuilder.Setup(builder => builder.BuildTenantDto(built)).Returns(dto);

        TenantDto result = await _sut.CreateTenant(new CreateTenantDto { Name = "Nueva Escuela" });

        Assert.That(result, Is.SameAs(dto));
        _tenantDao.Verify(dao => dao.CreateTenantAsync(built), Times.Once);
    }

    [Test]
    public async Task RenameTenant_WhenUpdateAffectsRows_ReturnsUpdated()
    {
        var targetTenantId = Guid.NewGuid();
        _tenantDao.Setup(dao => dao.UpdateNameAsync(targetTenantId, "Renombrado")).ReturnsAsync(1);

        UpdateTenantNameOutcome outcome = await _sut.RenameTenant(targetTenantId, "Renombrado");

        Assert.That(outcome, Is.InstanceOf<UpdateTenantNameOutcome.Updated>());
    }

    [Test]
    public async Task RenameTenant_WhenTenantNotFound_ReturnsNotFound()
    {
        var targetTenantId = Guid.NewGuid();
        _tenantDao.Setup(dao => dao.UpdateNameAsync(targetTenantId, "Renombrado")).ReturnsAsync(0);

        UpdateTenantNameOutcome outcome = await _sut.RenameTenant(targetTenantId, "Renombrado");

        Assert.That(outcome, Is.InstanceOf<UpdateTenantNameOutcome.NotFound>());
    }

    [Test]
    public async Task UpdateTenantTimezone_WhenCallerBelongsToDifferentTenant_ReturnsForbidden()
    {
        var targetTenantId = Guid.NewGuid();
        _claimContext.Setup(accessor => accessor.TenantId).Returns(CallerTenantId);

        UpdateTenantTimezoneOutcome outcome =
            await _sut.UpdateTenantTimezone(targetTenantId, "America/La_Paz");

        Assert.That(outcome, Is.InstanceOf<UpdateTenantTimezoneOutcome.Forbidden>());
        _tenantDao.Verify(
            dao => dao.UpdateTimezoneAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task UpdateTenantTimezone_WhenTenantNotFound_ReturnsNotFound()
    {
        _claimContext.Setup(accessor => accessor.TenantId).Returns(CallerTenantId);
        _tenantDao
            .Setup(dao => dao.UpdateTimezoneAsync(CallerTenantId, "America/Bogota"))
            .ReturnsAsync(0);

        UpdateTenantTimezoneOutcome outcome =
            await _sut.UpdateTenantTimezone(CallerTenantId, "America/Bogota");

        Assert.That(outcome, Is.InstanceOf<UpdateTenantTimezoneOutcome.NotFound>());
    }

    [Test]
    public async Task UpdateTenantTimezone_WhenUpdateAffectsRows_ReturnsUpdated()
    {
        _claimContext.Setup(accessor => accessor.TenantId).Returns(CallerTenantId);
        _tenantDao
            .Setup(dao => dao.UpdateTimezoneAsync(CallerTenantId, "Europe/Madrid"))
            .ReturnsAsync(1);

        UpdateTenantTimezoneOutcome outcome =
            await _sut.UpdateTenantTimezone(CallerTenantId, "Europe/Madrid");

        Assert.That(outcome, Is.InstanceOf<UpdateTenantTimezoneOutcome.Updated>());
    }

    [Test]
    public async Task GetTierDistribution_MapsRowsToDtos()
    {
        _tenantDao.Setup(dao => dao.GetCountBySubscriptionTierAsync())
                  .ReturnsAsync(new List<TenantTierCountRow>
                  {
                      new(1, 4),
                      new(3, 2)
                  });

        List<TenantTierCountDto> distribution = await _sut.GetTierDistribution();

        Assert.Multiple(() =>
        {
            Assert.That(distribution, Has.Count.EqualTo(2));
            Assert.That(distribution[0].Tier, Is.EqualTo(1));
            Assert.That(distribution[0].TenantCount, Is.EqualTo(4));
            Assert.That(distribution[1].Tier, Is.EqualTo(3));
            Assert.That(distribution[1].TenantCount, Is.EqualTo(2));
        });
    }
}
