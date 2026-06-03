using Backend.Application.Commands;
using Backend.Application.Handlers;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.DebtTemplates;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Dtos.External.Todotix;
using Backend.Dtos.QrPayments.Input;
using Backend.Dtos.QrPayments.Output;
using Backend.Entities;
using Backend.Entities.DebtTemplates;
using Backend.Entities.QrPayments;
using Backend.Entities.Todotix;
using Backend.Results.QrPayments;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

using Test.Infrastructure;

namespace Test.Services.Concrete.QrPayments;

[TestFixture]
public class CreateClassQrDebtCommandHandlerTests
{
    private Mock<IDebtTemplateDao> _debtTemplateDao = null!;
    private Mock<IPendingQrPaymentDao> _pendingDao = null!;
    private Mock<ITodotixOutboxDao> _todotixOutboxDao = null!;
    private Mock<IExpirationOutboxDao> _expirationOutboxDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<IQrPaymentCreationBuilder> _creationBuilder = null!;
    private Mock<Backend.Services.Abstract.Todotix.ITodotixAppKeyResolver> _appKeyResolver = null!;
    private CreateClassQrDebtCommandHandler _sut = null!;
    private Guid _tenantId;
    private Guid _studentId;

    [SetUp]
    public void Setup()
    {
        _debtTemplateDao = new Mock<IDebtTemplateDao>(MockBehavior.Strict);
        _pendingDao = new Mock<IPendingQrPaymentDao>(MockBehavior.Strict);
        _todotixOutboxDao = new Mock<ITodotixOutboxDao>(MockBehavior.Strict);
        _expirationOutboxDao = new Mock<IExpirationOutboxDao>(MockBehavior.Strict);
        (_unitOfWork, _transactionScope) = UnitOfWorkMockHelper.BuildCommittingMocks();
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _creationBuilder = new Mock<IQrPaymentCreationBuilder>(MockBehavior.Strict);
        _appKeyResolver = new Mock<Backend.Services.Abstract.Todotix.ITodotixAppKeyResolver>(MockBehavior.Strict);
        _appKeyResolver.Setup(r => r.ResolveAsync(It.IsAny<Guid>())).ReturnsAsync("tenant-app-key");

        _tenantId = Guid.NewGuid();
        _studentId = Guid.NewGuid();
        _claimContext.Setup(c => c.TenantId).Returns(_tenantId);
        _claimContext.Setup(c => c.UserId).Returns(_studentId);
        _claimContext.Setup(c => c.TenantTimezone).Returns("America/La_Paz");
        _claimContext.Setup(c => c.TenantName).Returns("Acme");

        _sut = new CreateClassQrDebtCommandHandler(
            _debtTemplateDao.Object,
            _pendingDao.Object,
            _todotixOutboxDao.Object,
            _expirationOutboxDao.Object,
            _unitOfWork.Object,
            _claimContext.Object,
            _creationBuilder.Object,
            _appKeyResolver.Object);
    }

    [Test]
    public async Task CreateDebtAsync_WhenTemplateMissing_ReturnsTemplateNotFound()
    {
        var templateId = Guid.NewGuid();
        _debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, templateId)).ReturnsAsync((DebtTemplate?)null);

        CreateQrDebtOutcome outcome = await _sut.Handle(new CreateClassQrDebtCommand(templateId, "a@b.com", new CreateQrDebtDto()));

        Assert.That(outcome, Is.TypeOf<CreateQrDebtOutcome.TemplateNotFound>());
    }

    [Test]
    public async Task CreateDebtAsync_WhenActiveDebtExists_ReturnsSuccessWithAlreadyGenerated()
    {
        var templateId = Guid.NewGuid();
        var existingDebtId = Guid.NewGuid();
        var template = new DebtTemplate { Id = templateId, TenantId = _tenantId, Description = "d", ClassQuantity = 1, Cost = 10 };
        _debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, templateId)).ReturnsAsync(template);
        _pendingDao.Setup(d => d.GetActiveForTemplateAsync(_tenantId, _studentId, templateId, It.IsAny<DateTime>())).ReturnsAsync(existingDebtId);
        _creationBuilder.Setup(b => b.BuildPendingDebtDto(existingDebtId, true))
            .Returns(new QrDebtPendingDto { IdentificadorDeuda = existingDebtId, Status = "Pending", AlreadyGenerated = true });

        CreateQrDebtOutcome outcome = await _sut.Handle(new CreateClassQrDebtCommand(templateId, "a@b.com", new CreateQrDebtDto()));

        Assert.That(outcome, Is.TypeOf<CreateQrDebtOutcome.Success>());
        var success = (CreateQrDebtOutcome.Success)outcome;
        Assert.Multiple(() =>
        {
            Assert.That(success.Created.AlreadyGenerated, Is.True);
            Assert.That(success.Created.IdentificadorDeuda, Is.EqualTo(existingDebtId));
        });
    }

    [Test]
    public async Task CreateDebtAsync_WhenTenantHasNoCredential_ReturnsPaymentNotConfigured()
    {
        var templateId = Guid.NewGuid();
        var template = new DebtTemplate { Id = templateId, TenantId = _tenantId, Description = "d", ClassQuantity = 1, Cost = 10 };
        _debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, templateId)).ReturnsAsync(template);
        _pendingDao.Setup(d => d.GetActiveForTemplateAsync(_tenantId, _studentId, templateId, It.IsAny<DateTime>())).ReturnsAsync((Guid?)null);
        _appKeyResolver.Setup(r => r.ResolveAsync(It.IsAny<Guid>())).ReturnsAsync((string?)null);

        CreateQrDebtOutcome outcome = await _sut.Handle(new CreateClassQrDebtCommand(templateId, "a@b.com", new CreateQrDebtDto()));

        Assert.That(outcome, Is.TypeOf<CreateQrDebtOutcome.PaymentNotConfigured>());
    }

    [Test]
    public async Task CreateDebtAsync_HappyPath_ReturnsSuccessAndCommits()
    {
        var templateId = Guid.NewGuid();
        var template = new DebtTemplate { Id = templateId, TenantId = _tenantId, Description = "d", ClassQuantity = 1, Cost = 10 };
        _debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, templateId)).ReturnsAsync(template);
        _pendingDao.Setup(d => d.GetActiveForTemplateAsync(_tenantId, _studentId, templateId, It.IsAny<DateTime>())).ReturnsAsync((Guid?)null);

        var pending = new PendingQrPayment { Id = Guid.NewGuid(), TenantId = _tenantId, StudentId = _studentId };
        var todotixRequest = new RegisterDebtRequest();
        var todotixEvent = new TodotixOutboxEvent { Id = Guid.NewGuid(), PendingId = pending.Id, TenantId = _tenantId };
        var expirationEvent = new ExpirationOutboxEvent { Id = Guid.NewGuid(), AggregateId = pending.Id };
        var pendingDto = new QrDebtPendingDto { IdentificadorDeuda = pending.Id, Status = "Pending" };

        _creationBuilder.Setup(b => b.BuildPendingPayment(It.IsAny<Guid>(), _tenantId, _studentId, templateId, template, It.IsAny<DateTime>())).Returns(pending);
        _creationBuilder.Setup(b => b.BuildTodotixRequest(It.IsAny<Guid>(), "a@b.com", template, "America/La_Paz", It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>())).Returns(todotixRequest);
        _creationBuilder.Setup(b => b.BuildOutboxEvent(It.IsAny<Guid>(), _tenantId, todotixRequest)).Returns(todotixEvent);
        _creationBuilder.Setup(b => b.BuildExpirationOutboxEvent(It.IsAny<Guid>(), _tenantId, _studentId, It.IsAny<DateTime>())).Returns(expirationEvent);
        _creationBuilder.Setup(b => b.BuildPendingDebtDto(It.IsAny<Guid>(), false)).Returns(pendingDto);

        _pendingDao.Setup(d => d.CreateAsync(pending, _transactionScope.Object)).Returns(Task.CompletedTask);
        _todotixOutboxDao.Setup(d => d.InsertAsync(todotixEvent, _transactionScope.Object)).Returns(Task.CompletedTask);
        _expirationOutboxDao.Setup(d => d.InsertAsync(expirationEvent, _transactionScope.Object)).Returns(Task.CompletedTask);

        CreateQrDebtOutcome outcome = await _sut.Handle(new CreateClassQrDebtCommand(templateId, "a@b.com", new CreateQrDebtDto()));

        Assert.That(outcome, Is.TypeOf<CreateQrDebtOutcome.Success>());
        _transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }
}
