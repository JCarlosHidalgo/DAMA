using Backend.Builders;
using Backend.Dtos.Tenants.Output;
using Backend.Entities.Tenants;

namespace Test.Builders;

[TestFixture]
public class TenantBuilderTests
{
    private const string ExpectedDefaultTimezone = "America/La_Paz";

    private TenantBuilder _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new TenantBuilder();

    [Test]
    public void BuildTenant_WithGivenName_AssignsNameDefaultTimezoneAndFreshId()
    {
        Tenant tenant = _sut.BuildTenant("Sample Tenant");

        Assert.Multiple(() =>
        {
            Assert.That(tenant.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(tenant.Name, Is.EqualTo("Sample Tenant"));
            Assert.That(tenant.Timezone, Is.EqualTo(ExpectedDefaultTimezone));
        });
    }

    [Test]
    public void BuildTenant_CalledTwice_ProducesDistinctIds()
    {
        Tenant first = _sut.BuildTenant("Sample Tenant");
        Tenant second = _sut.BuildTenant("Sample Tenant");

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [Test]
    public void BuildTenantDto_WithGivenTenant_CopiesAllFields()
    {
        Tenant tenant = new()
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Sample Tenant",
            Timezone = "America/Lima"
        };

        TenantDto tenantDto = _sut.BuildTenantDto(tenant);

        Assert.Multiple(() =>
        {
            Assert.That(tenantDto.Id, Is.EqualTo(tenant.Id));
            Assert.That(tenantDto.Name, Is.EqualTo(tenant.Name));
            Assert.That(tenantDto.Timezone, Is.EqualTo(tenant.Timezone));
        });
    }

    [Test]
    public void BuildTenantDtos_WithGivenTenants_MapsEachInOrder()
    {
        Tenant first = new()
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "First Tenant",
            Timezone = "America/Lima"
        };
        Tenant second = new()
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Name = "Second Tenant",
            Timezone = "America/La_Paz"
        };

        List<TenantDto> tenantDtos = _sut.BuildTenantDtos([first, second]);

        Assert.Multiple(() =>
        {
            Assert.That(tenantDtos, Has.Count.EqualTo(2));
            Assert.That(tenantDtos[0].Id, Is.EqualTo(first.Id));
            Assert.That(tenantDtos[0].Name, Is.EqualTo(first.Name));
            Assert.That(tenantDtos[0].Timezone, Is.EqualTo(first.Timezone));
            Assert.That(tenantDtos[1].Id, Is.EqualTo(second.Id));
            Assert.That(tenantDtos[1].Name, Is.EqualTo(second.Name));
            Assert.That(tenantDtos[1].Timezone, Is.EqualTo(second.Timezone));
        });
    }

    [Test]
    public void BuildTenantDtos_WithEmptySequence_ReturnsEmptyList()
    {
        List<TenantDto> tenantDtos = _sut.BuildTenantDtos([]);

        Assert.That(tenantDtos, Is.Empty);
    }
}
