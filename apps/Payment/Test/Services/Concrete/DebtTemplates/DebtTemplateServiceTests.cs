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
    private Mock<IDebtTemplateDao> _debtTemplateDao = null!;
    private Mock<IQrPaymentIdempotencyDao> _idempotencyDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private IMapper _autoMapper = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IDebtTemplateBuilder> _debtTemplateBuilder = null!;
    private DebtTemplateService _sut = null!;
    private Guid _tenantId;

    [SetUp]
    public void Setup()
    {
        _debtTemplateDao = new Mock<IDebtTemplateDao>(MockBehavior.Strict);
        _idempotencyDao = new Mock<IQrPaymentIdempotencyDao>(MockBehavior.Strict);
        (_unitOfWork, _transactionScope) = UnitOfWorkMockHelper.BuildCommittingMocks();

        var mapperConfiguration = new MapperConfiguration(
            configuration => configuration.AddProfile<DebtTemplateProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _autoMapper = mapperConfiguration.CreateMapper();

        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _debtTemplateBuilder = new Mock<IDebtTemplateBuilder>(MockBehavior.Strict);

        _tenantId = Guid.NewGuid();
        _claimContext.Setup(c => c.TenantId).Returns(_tenantId);

        _sut = new DebtTemplateService(
            _debtTemplateDao.Object,
            _idempotencyDao.Object,
            _unitOfWork.Object,
            _autoMapper,
            _claimContext.Object,
            _debtTemplateBuilder.Object);
    }

    [Test]
    public async Task CreateAsync_WithoutExternalReference_InsertsAndReturnsSuccess()
    {
        var request = new CreateDebtTemplateDto { Description = "x", ClassQuantity = 1, Cost = 1 };
        var candidate = new DebtTemplate { Id = Guid.NewGuid(), TenantId = _tenantId, Description = "x", ClassQuantity = 1, Cost = 1 };
        _debtTemplateBuilder.Setup(b => b.BuildDebtTemplate(_tenantId, request)).Returns(candidate);
        _debtTemplateDao.Setup(d => d.CreateAsync(candidate)).Returns(Task.CompletedTask);

        CreateDebtTemplateOutcome outcome = await _sut.CreateAsync(request);

        Assert.That(outcome, Is.TypeOf<CreateDebtTemplateOutcome.Success>());
        var success = (CreateDebtTemplateOutcome.Success)outcome;
        Assert.That(success.Created.Id, Is.EqualTo(candidate.Id));
    }

    [Test]
    public async Task CreateAsync_WithExternalReference_FirstInsertReturnsSuccess()
    {
        var request = new CreateDebtTemplateDto { Description = "x", ClassQuantity = 1, Cost = 1, ExternalReference = "ref-1" };
        var candidate = new DebtTemplate { Id = Guid.NewGuid(), TenantId = _tenantId, Description = "x", ClassQuantity = 1, Cost = 1 };
        var idempotencyRecord = new QrPaymentIdempotency { TenantId = _tenantId, ExternalReference = "ref-1", EntityId = candidate.Id };
        _debtTemplateBuilder.Setup(b => b.BuildDebtTemplate(_tenantId, request)).Returns(candidate);
        _debtTemplateBuilder.Setup(b => b.BuildIdempotencyRecord(_tenantId, "ref-1", candidate.Id)).Returns(idempotencyRecord);
        _idempotencyDao.Setup(d => d.TryRecordAsync(idempotencyRecord, _transactionScope.Object)).ReturnsAsync(true);
        _debtTemplateDao.Setup(d => d.CreateAsync(candidate, _transactionScope.Object)).Returns(Task.CompletedTask);

        CreateDebtTemplateOutcome outcome = await _sut.CreateAsync(request);

        Assert.That(outcome, Is.TypeOf<CreateDebtTemplateOutcome.Success>());
        _transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task CreateAsync_WithExternalReference_DuplicateReturnsReplayed()
    {
        var request = new CreateDebtTemplateDto { Description = "x", ClassQuantity = 1, Cost = 1, ExternalReference = "ref-1" };
        var candidate = new DebtTemplate { Id = Guid.NewGuid(), TenantId = _tenantId, Description = "x", ClassQuantity = 1, Cost = 1 };
        var previous = new DebtTemplate { Id = Guid.NewGuid(), TenantId = _tenantId, Description = "prev", ClassQuantity = 1, Cost = 1 };
        var record = new QrPaymentIdempotency { TenantId = _tenantId, ExternalReference = "ref-1", EntityId = previous.Id };
        _debtTemplateBuilder.Setup(b => b.BuildDebtTemplate(_tenantId, request)).Returns(candidate);
        _debtTemplateBuilder.Setup(b => b.BuildIdempotencyRecord(_tenantId, "ref-1", candidate.Id)).Returns(record);
        _idempotencyDao.Setup(d => d.TryRecordAsync(record, _transactionScope.Object)).ReturnsAsync(false);
        _idempotencyDao.Setup(d => d.GetByExternalReferenceAsync(_tenantId, "ref-1")).ReturnsAsync(record);
        _debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, record.EntityId)).ReturnsAsync(previous);

        CreateDebtTemplateOutcome outcome = await _sut.CreateAsync(request);

        Assert.That(outcome, Is.TypeOf<CreateDebtTemplateOutcome.Replayed>());
        var replayed = (CreateDebtTemplateOutcome.Replayed)outcome;
        Assert.That(replayed.Existing.Id, Is.EqualTo(previous.Id));
        _transactionScope.Verify(s => s.CommitAsync(), Times.Never);
    }

    [Test]
    public void CreateAsync_WithExternalReference_DuplicateButMissingIdempotency_Throws()
    {
        var request = new CreateDebtTemplateDto { Description = "x", ClassQuantity = 1, Cost = 1, ExternalReference = "ref-1" };
        var candidate = new DebtTemplate { Id = Guid.NewGuid(), TenantId = _tenantId, Description = "x", ClassQuantity = 1, Cost = 1 };
        var record = new QrPaymentIdempotency { TenantId = _tenantId, ExternalReference = "ref-1", EntityId = candidate.Id };
        _debtTemplateBuilder.Setup(b => b.BuildDebtTemplate(_tenantId, request)).Returns(candidate);
        _debtTemplateBuilder.Setup(b => b.BuildIdempotencyRecord(_tenantId, "ref-1", candidate.Id)).Returns(record);
        _idempotencyDao.Setup(d => d.TryRecordAsync(record, _transactionScope.Object)).ReturnsAsync(false);
        _idempotencyDao.Setup(d => d.GetByExternalReferenceAsync(_tenantId, "ref-1")).ReturnsAsync((QrPaymentIdempotency?)null);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
    }

    [Test]
    public void CreateAsync_WithExternalReference_DuplicateButMissingTemplate_Throws()
    {
        var request = new CreateDebtTemplateDto { Description = "x", ClassQuantity = 1, Cost = 1, ExternalReference = "ref-1" };
        var candidate = new DebtTemplate { Id = Guid.NewGuid(), TenantId = _tenantId, Description = "x", ClassQuantity = 1, Cost = 1 };
        var record = new QrPaymentIdempotency { TenantId = _tenantId, ExternalReference = "ref-1", EntityId = candidate.Id };
        _debtTemplateBuilder.Setup(b => b.BuildDebtTemplate(_tenantId, request)).Returns(candidate);
        _debtTemplateBuilder.Setup(b => b.BuildIdempotencyRecord(_tenantId, "ref-1", candidate.Id)).Returns(record);
        _idempotencyDao.Setup(d => d.TryRecordAsync(record, _transactionScope.Object)).ReturnsAsync(false);
        _idempotencyDao.Setup(d => d.GetByExternalReferenceAsync(_tenantId, "ref-1")).ReturnsAsync(record);
        _debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, record.EntityId)).ReturnsAsync((DebtTemplate?)null);

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
    }

    [Test]
    public async Task GetByTenantAsync_MapsListToDtos()
    {
        var one = new DebtTemplate { Id = Guid.NewGuid(), TenantId = _tenantId, Description = "a", ClassQuantity = 1, Cost = 1 };
        var two = new DebtTemplate { Id = Guid.NewGuid(), TenantId = _tenantId, Description = "b", ClassQuantity = 2, Cost = 2 };
        _debtTemplateDao.Setup(d => d.GetByTenantAsync(_tenantId)).ReturnsAsync([one, two]);

        List<DebtTemplateDto> result = await _sut.GetByTenantAsync();

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
        var template = new DebtTemplate { Id = templateId, TenantId = _tenantId, Description = "t", ClassQuantity = 5, Cost = 100 };
        _debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, templateId)).ReturnsAsync(template);

        GetDebtTemplateOutcome outcome = await _sut.GetByIdAsync(templateId);

        Assert.That(outcome, Is.TypeOf<GetDebtTemplateOutcome.Found>());
    }

    [Test]
    public async Task GetByIdAsync_WhenMissing_ReturnsNotFound()
    {
        var templateId = Guid.NewGuid();
        _debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, templateId)).ReturnsAsync((DebtTemplate?)null);

        GetDebtTemplateOutcome outcome = await _sut.GetByIdAsync(templateId);

        Assert.That(outcome, Is.TypeOf<GetDebtTemplateOutcome.NotFound>());
    }

    [Test]
    public async Task UpdateAsync_WhenRowsAffected_ReturnsUpdated()
    {
        var templateId = Guid.NewGuid();
        var request = new UpdateDebtTemplateDto { Description = "u", ClassQuantity = 4, Cost = 80 };
        _debtTemplateDao.Setup(d => d.UpdateForTenantAsync(_tenantId, templateId, "u", 4, 80)).ReturnsAsync(true);

        UpdateDebtTemplateOutcome outcome = await _sut.UpdateAsync(templateId, request);

        Assert.That(outcome, Is.TypeOf<UpdateDebtTemplateOutcome.Updated>());
    }

    [Test]
    public async Task UpdateAsync_WhenNoneAffected_ReturnsNotFound()
    {
        var templateId = Guid.NewGuid();
        var request = new UpdateDebtTemplateDto { Description = "u", ClassQuantity = 4, Cost = 80 };
        _debtTemplateDao.Setup(d => d.UpdateForTenantAsync(_tenantId, templateId, "u", 4, 80)).ReturnsAsync(false);

        UpdateDebtTemplateOutcome outcome = await _sut.UpdateAsync(templateId, request);

        Assert.That(outcome, Is.TypeOf<UpdateDebtTemplateOutcome.NotFound>());
    }

    [Test]
    public async Task DeleteAsync_WhenAffected_ReturnsDeleted()
    {
        var templateId = Guid.NewGuid();
        _debtTemplateDao.Setup(d => d.DeleteForTenantAsync(_tenantId, templateId)).ReturnsAsync(true);

        DeleteDebtTemplateOutcome outcome = await _sut.DeleteAsync(templateId);

        Assert.That(outcome, Is.TypeOf<DeleteDebtTemplateOutcome.Deleted>());
    }

    [Test]
    public async Task DeleteAsync_WhenNotAffected_ReturnsNotFound()
    {
        var templateId = Guid.NewGuid();
        _debtTemplateDao.Setup(d => d.DeleteForTenantAsync(_tenantId, templateId)).ReturnsAsync(false);

        DeleteDebtTemplateOutcome outcome = await _sut.DeleteAsync(templateId);

        Assert.That(outcome, Is.TypeOf<DeleteDebtTemplateOutcome.NotFound>());
    }
}
