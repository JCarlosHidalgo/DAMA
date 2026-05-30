using AutoMapper;

using Backend.AutoMapperProfiles;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.DebtTemplates;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Dtos.DebtTemplates.Input;
using Backend.Dtos.DebtTemplates.Output;
using Backend.Entities.DebtTemplates;
using Backend.Entities.QrPayments;
using Backend.Results.DebtTemplates;
using Backend.Services.Concrete.DebtTemplates;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

using Test.Infrastructure;

namespace Test.Services.Concrete.DebtTemplates;

[TestFixture]
public class DebtTemplateServiceTests
{
    private Mock<IDebtTemplateDao> debtTemplateDao = null!;
    private Mock<IQrPaymentIdempotencyDao> idempotencyDao = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private IMapper autoMapper = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<IDebtTemplateBuilder> debtTemplateBuilder = null!;
    private DebtTemplateService sut = null!;
    private Guid tenantId;

    [SetUp]
    public void Setup()
    {
        debtTemplateDao = new Mock<IDebtTemplateDao>(MockBehavior.Strict);
        idempotencyDao = new Mock<IQrPaymentIdempotencyDao>(MockBehavior.Strict);
        (unitOfWork, transactionScope) = UnitOfWorkMockHelper.BuildCommittingMocks();

        var mapperConfiguration = new MapperConfiguration(
            configuration => configuration.AddProfile<DebtTemplateProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        autoMapper = mapperConfiguration.CreateMapper();

        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        debtTemplateBuilder = new Mock<IDebtTemplateBuilder>(MockBehavior.Strict);

        tenantId = Guid.NewGuid();
        claimContext.Setup(c => c.TenantId).Returns(tenantId);

        sut = new DebtTemplateService(
            debtTemplateDao.Object,
            idempotencyDao.Object,
            unitOfWork.Object,
            autoMapper,
            claimContext.Object,
            debtTemplateBuilder.Object);
    }

    [Test]
    public async Task CreateAsync_WithoutExternalReference_InsertsAndReturnsSuccess()
    {
        var request = new CreateDebtTemplateDto { Description = "x", ClassQuantity = 1, Cost = 1 };
        var candidate = new DebtTemplate { Id = Guid.NewGuid(), TenantId = tenantId, Description = "x", ClassQuantity = 1, Cost = 1 };
        debtTemplateBuilder.Setup(b => b.BuildDebtTemplate(tenantId, request)).Returns(candidate);
        debtTemplateDao.Setup(d => d.CreateAsync(candidate)).Returns(Task.CompletedTask);

        CreateDebtTemplateOutcome outcome = await sut.CreateAsync(request);

        Assert.That(outcome, Is.TypeOf<CreateDebtTemplateOutcome.Success>());
        var success = (CreateDebtTemplateOutcome.Success)outcome;
        Assert.That(success.Created.Id, Is.EqualTo(candidate.Id));
    }

    [Test]
    public async Task CreateAsync_WithExternalReference_FirstInsertReturnsSuccess()
    {
        var request = new CreateDebtTemplateDto { Description = "x", ClassQuantity = 1, Cost = 1, ExternalReference = "ref-1" };
        var candidate = new DebtTemplate { Id = Guid.NewGuid(), TenantId = tenantId, Description = "x", ClassQuantity = 1, Cost = 1 };
        var idempotencyRecord = new QrPaymentIdempotency { TenantId = tenantId, ExternalReference = "ref-1", EntityId = candidate.Id };
        debtTemplateBuilder.Setup(b => b.BuildDebtTemplate(tenantId, request)).Returns(candidate);
        debtTemplateBuilder.Setup(b => b.BuildIdempotencyRecord(tenantId, "ref-1", candidate.Id)).Returns(idempotencyRecord);
        idempotencyDao.Setup(d => d.TryRecordAsync(idempotencyRecord, transactionScope.Object)).ReturnsAsync(true);
        debtTemplateDao.Setup(d => d.CreateAsync(candidate, transactionScope.Object)).Returns(Task.CompletedTask);

        CreateDebtTemplateOutcome outcome = await sut.CreateAsync(request);

        Assert.That(outcome, Is.TypeOf<CreateDebtTemplateOutcome.Success>());
        transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task CreateAsync_WithExternalReference_DuplicateReturnsReplayed()
    {
        var request = new CreateDebtTemplateDto { Description = "x", ClassQuantity = 1, Cost = 1, ExternalReference = "ref-1" };
        var candidate = new DebtTemplate { Id = Guid.NewGuid(), TenantId = tenantId, Description = "x", ClassQuantity = 1, Cost = 1 };
        var previous = new DebtTemplate { Id = Guid.NewGuid(), TenantId = tenantId, Description = "prev", ClassQuantity = 1, Cost = 1 };
        var record = new QrPaymentIdempotency { TenantId = tenantId, ExternalReference = "ref-1", EntityId = previous.Id };
        debtTemplateBuilder.Setup(b => b.BuildDebtTemplate(tenantId, request)).Returns(candidate);
        debtTemplateBuilder.Setup(b => b.BuildIdempotencyRecord(tenantId, "ref-1", candidate.Id)).Returns(record);
        idempotencyDao.Setup(d => d.TryRecordAsync(record, transactionScope.Object)).ReturnsAsync(false);
        idempotencyDao.Setup(d => d.GetByExternalReferenceAsync(tenantId, "ref-1")).ReturnsAsync(record);
        debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(tenantId, record.EntityId)).ReturnsAsync(previous);

        CreateDebtTemplateOutcome outcome = await sut.CreateAsync(request);

        Assert.That(outcome, Is.TypeOf<CreateDebtTemplateOutcome.Replayed>());
        var replayed = (CreateDebtTemplateOutcome.Replayed)outcome;
        Assert.That(replayed.Existing.Id, Is.EqualTo(previous.Id));
        transactionScope.Verify(s => s.CommitAsync(), Times.Never);
    }

    [Test]
    public void CreateAsync_WithExternalReference_DuplicateButMissingIdempotency_Throws()
    {
        var request = new CreateDebtTemplateDto { Description = "x", ClassQuantity = 1, Cost = 1, ExternalReference = "ref-1" };
        var candidate = new DebtTemplate { Id = Guid.NewGuid(), TenantId = tenantId, Description = "x", ClassQuantity = 1, Cost = 1 };
        var record = new QrPaymentIdempotency { TenantId = tenantId, ExternalReference = "ref-1", EntityId = candidate.Id };
        debtTemplateBuilder.Setup(b => b.BuildDebtTemplate(tenantId, request)).Returns(candidate);
        debtTemplateBuilder.Setup(b => b.BuildIdempotencyRecord(tenantId, "ref-1", candidate.Id)).Returns(record);
        idempotencyDao.Setup(d => d.TryRecordAsync(record, transactionScope.Object)).ReturnsAsync(false);
        idempotencyDao.Setup(d => d.GetByExternalReferenceAsync(tenantId, "ref-1")).ReturnsAsync((QrPaymentIdempotency?)null);

        Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(request));
    }

    [Test]
    public void CreateAsync_WithExternalReference_DuplicateButMissingTemplate_Throws()
    {
        var request = new CreateDebtTemplateDto { Description = "x", ClassQuantity = 1, Cost = 1, ExternalReference = "ref-1" };
        var candidate = new DebtTemplate { Id = Guid.NewGuid(), TenantId = tenantId, Description = "x", ClassQuantity = 1, Cost = 1 };
        var record = new QrPaymentIdempotency { TenantId = tenantId, ExternalReference = "ref-1", EntityId = candidate.Id };
        debtTemplateBuilder.Setup(b => b.BuildDebtTemplate(tenantId, request)).Returns(candidate);
        debtTemplateBuilder.Setup(b => b.BuildIdempotencyRecord(tenantId, "ref-1", candidate.Id)).Returns(record);
        idempotencyDao.Setup(d => d.TryRecordAsync(record, transactionScope.Object)).ReturnsAsync(false);
        idempotencyDao.Setup(d => d.GetByExternalReferenceAsync(tenantId, "ref-1")).ReturnsAsync(record);
        debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(tenantId, record.EntityId)).ReturnsAsync((DebtTemplate?)null);

        Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(request));
    }

    [Test]
    public async Task GetByTenantAsync_MapsListToDtos()
    {
        var one = new DebtTemplate { Id = Guid.NewGuid(), TenantId = tenantId, Description = "a", ClassQuantity = 1, Cost = 1 };
        var two = new DebtTemplate { Id = Guid.NewGuid(), TenantId = tenantId, Description = "b", ClassQuantity = 2, Cost = 2 };
        debtTemplateDao.Setup(d => d.GetByTenantAsync(tenantId)).ReturnsAsync([one, two]);

        List<DebtTemplateDto> result = await sut.GetByTenantAsync();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Id, Is.EqualTo(one.Id));
            Assert.That(result[1].Id, Is.EqualTo(two.Id));
        });
    }

    [Test]
    public async Task GetByIdAsync_WhenExists_ReturnsFound()
    {
        var templateId = Guid.NewGuid();
        var template = new DebtTemplate { Id = templateId, TenantId = tenantId, Description = "t", ClassQuantity = 5, Cost = 100 };
        debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(tenantId, templateId)).ReturnsAsync(template);

        GetDebtTemplateOutcome outcome = await sut.GetByIdAsync(templateId);

        Assert.That(outcome, Is.TypeOf<GetDebtTemplateOutcome.Found>());
    }

    [Test]
    public async Task GetByIdAsync_WhenMissing_ReturnsNotFound()
    {
        var templateId = Guid.NewGuid();
        debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(tenantId, templateId)).ReturnsAsync((DebtTemplate?)null);

        GetDebtTemplateOutcome outcome = await sut.GetByIdAsync(templateId);

        Assert.That(outcome, Is.TypeOf<GetDebtTemplateOutcome.NotFound>());
    }

    [Test]
    public async Task UpdateAsync_WhenRowsAffected_ReturnsUpdated()
    {
        var templateId = Guid.NewGuid();
        var request = new UpdateDebtTemplateDto { Description = "u", ClassQuantity = 4, Cost = 80 };
        debtTemplateDao.Setup(d => d.UpdateForTenantAsync(tenantId, templateId, "u", 4, 80)).ReturnsAsync(true);

        UpdateDebtTemplateOutcome outcome = await sut.UpdateAsync(templateId, request);

        Assert.That(outcome, Is.TypeOf<UpdateDebtTemplateOutcome.Updated>());
    }

    [Test]
    public async Task UpdateAsync_WhenNoneAffected_ReturnsNotFound()
    {
        var templateId = Guid.NewGuid();
        var request = new UpdateDebtTemplateDto { Description = "u", ClassQuantity = 4, Cost = 80 };
        debtTemplateDao.Setup(d => d.UpdateForTenantAsync(tenantId, templateId, "u", 4, 80)).ReturnsAsync(false);

        UpdateDebtTemplateOutcome outcome = await sut.UpdateAsync(templateId, request);

        Assert.That(outcome, Is.TypeOf<UpdateDebtTemplateOutcome.NotFound>());
    }

    [Test]
    public async Task DeleteAsync_WhenAffected_ReturnsDeleted()
    {
        var templateId = Guid.NewGuid();
        debtTemplateDao.Setup(d => d.DeleteForTenantAsync(tenantId, templateId)).ReturnsAsync(true);

        DeleteDebtTemplateOutcome outcome = await sut.DeleteAsync(templateId);

        Assert.That(outcome, Is.TypeOf<DeleteDebtTemplateOutcome.Deleted>());
    }

    [Test]
    public async Task DeleteAsync_WhenNotAffected_ReturnsNotFound()
    {
        var templateId = Guid.NewGuid();
        debtTemplateDao.Setup(d => d.DeleteForTenantAsync(tenantId, templateId)).ReturnsAsync(false);

        DeleteDebtTemplateOutcome outcome = await sut.DeleteAsync(templateId);

        Assert.That(outcome, Is.TypeOf<DeleteDebtTemplateOutcome.NotFound>());
    }
}
