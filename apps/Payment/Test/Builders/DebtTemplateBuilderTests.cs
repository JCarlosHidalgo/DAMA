using Backend.Builders;
using Backend.Dtos.DebtTemplates.Input;
using Backend.Entities.DebtTemplates;
using Backend.Entities.QrPayments;
using Backend.Options;

using Microsoft.Extensions.Options;

namespace Test.Builders;

[TestFixture]
public class DebtTemplateBuilderTests
{
    private DebtTemplateBuilder _sut = null!;

    [SetUp]
    public void Setup() => _sut = new DebtTemplateBuilder(Options.Create(new CurrencyOptions()));

    [Test]
    public void BuildDebtTemplate_WithValidInputs_PopulatesAllFieldsAndGeneratesId()
    {
        var tenantId = Guid.NewGuid();
        var request = new CreateDebtTemplateDto
        {
            Description = "Cuota anual",
            ClassQuantity = 12,
            Cost = 1200
        };

        DebtTemplate result = _sut.BuildDebtTemplate(tenantId, request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.TenantId, Is.EqualTo(tenantId));
            Assert.That(result.Description, Is.EqualTo("Cuota anual"));
            Assert.That(result.ClassQuantity, Is.EqualTo(12));
            Assert.That(result.Cost, Is.EqualTo(1200));
            Assert.That(result.Currency, Is.EqualTo("BOB"));
        });
    }

    [Test]
    public void BuildIdempotencyRecord_WithReference_CarriesExactValues()
    {
        var tenantId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        QrPaymentIdempotency record = _sut.BuildIdempotencyRecord(tenantId, "ref-001", entityId);

        Assert.Multiple(() =>
        {
            Assert.That(record.TenantId, Is.EqualTo(tenantId));
            Assert.That(record.ExternalReference, Is.EqualTo("ref-001"));
            Assert.That(record.EntityId, Is.EqualTo(entityId));
        });
    }
}
