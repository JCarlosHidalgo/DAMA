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
using Backend.Services.Concrete.QrPayments;

using DAMA.Software.MySqlUnitOfWork;

using Moq;

using Test.Infrastructure;

namespace Test.Services.Concrete.QrPayments;

[TestFixture]
public class QrDebtCreationServiceTests
{
    private Mock<IDebtTemplateDao> debtTemplateDao = null!;
    private Mock<IPendingQrPaymentDao> pendingDao = null!;
    private Mock<ITodotixOutboxDao> todotixOutboxDao = null!;
    private Mock<IExpirationOutboxDao> expirationOutboxDao = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<IQrPaymentCreationBuilder> creationBuilder = null!;
    private Mock<Backend.Services.Abstract.Todotix.ITodotixAppKeyResolver> appKeyResolver = null!;
    private QrDebtCreationService sut = null!;
    private Guid tenantId;
    private Guid studentId;

    [SetUp]
    public void Setup()
    {
        debtTemplateDao = new Mock<IDebtTemplateDao>(MockBehavior.Strict);
        pendingDao = new Mock<IPendingQrPaymentDao>(MockBehavior.Strict);
        todotixOutboxDao = new Mock<ITodotixOutboxDao>(MockBehavior.Strict);
        expirationOutboxDao = new Mock<IExpirationOutboxDao>(MockBehavior.Strict);
        (unitOfWork, transactionScope) = UnitOfWorkMockHelper.BuildCommittingMocks();
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        creationBuilder = new Mock<IQrPaymentCreationBuilder>(MockBehavior.Strict);
        appKeyResolver = new Mock<Backend.Services.Abstract.Todotix.ITodotixAppKeyResolver>(MockBehavior.Strict);
        appKeyResolver.Setup(r => r.ResolveAsync(It.IsAny<Guid>())).ReturnsAsync("tenant-app-key");

        tenantId = Guid.NewGuid();
        studentId = Guid.NewGuid();
        claimContext.Setup(c => c.TenantId).Returns(tenantId);
        claimContext.Setup(c => c.UserId).Returns(studentId);
        claimContext.Setup(c => c.TenantTimezone).Returns("America/La_Paz");
        claimContext.Setup(c => c.TenantName).Returns("Acme");

        sut = new QrDebtCreationService(
            debtTemplateDao.Object,
            pendingDao.Object,
            todotixOutboxDao.Object,
            expirationOutboxDao.Object,
            unitOfWork.Object,
            claimContext.Object,
            creationBuilder.Object,
            appKeyResolver.Object);
    }

    [Test]
    public async Task CreateDebtAsync_WhenTemplateMissing_ReturnsTemplateNotFound()
    {
        var templateId = Guid.NewGuid();
        debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(tenantId, templateId)).ReturnsAsync((DebtTemplate?)null);

        CreateQrDebtOutcome outcome = await sut.CreateDebtAsync(templateId, "a@b.com", new CreateQrDebtDto());

        Assert.That(outcome, Is.TypeOf<CreateQrDebtOutcome.TemplateNotFound>());
    }

    [Test]
    public async Task CreateDebtAsync_WhenActiveDebtExists_ReturnsActiveDebtForTemplate()
    {
        var templateId = Guid.NewGuid();
        var template = new DebtTemplate { Id = templateId, TenantId = tenantId, Description = "d", ClassQuantity = 1, Cost = 10 };
        debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(tenantId, templateId)).ReturnsAsync(template);
        pendingDao.Setup(d => d.CountActiveForTemplateAsync(tenantId, studentId, templateId, It.IsAny<DateTime>())).ReturnsAsync(1);

        CreateQrDebtOutcome outcome = await sut.CreateDebtAsync(templateId, "a@b.com", new CreateQrDebtDto());

        Assert.That(outcome, Is.TypeOf<CreateQrDebtOutcome.ActiveDebtForTemplate>());
    }

    [Test]
    public async Task CreateDebtAsync_HappyPath_ReturnsSuccessAndCommits()
    {
        var templateId = Guid.NewGuid();
        var template = new DebtTemplate { Id = templateId, TenantId = tenantId, Description = "d", ClassQuantity = 1, Cost = 10 };
        debtTemplateDao.Setup(d => d.GetByIdForTenantAsync(tenantId, templateId)).ReturnsAsync(template);
        pendingDao.Setup(d => d.CountActiveForTemplateAsync(tenantId, studentId, templateId, It.IsAny<DateTime>())).ReturnsAsync(0);

        var pending = new PendingQrPayment { Id = Guid.NewGuid(), TenantId = tenantId, StudentId = studentId };
        var todotixRequest = new RegisterDebtRequest();
        var todotixEvent = new TodotixOutboxEvent { Id = Guid.NewGuid(), PendingId = pending.Id, TenantId = tenantId };
        var expirationEvent = new ExpirationOutboxEvent { Id = Guid.NewGuid(), AggregateId = pending.Id };
        var pendingDto = new QrDebtPendingDto { IdentificadorDeuda = pending.Id, Status = "Pending" };

        creationBuilder.Setup(b => b.BuildPendingPayment(It.IsAny<Guid>(), tenantId, studentId, templateId, template, It.IsAny<DateTime>())).Returns(pending);
        creationBuilder.Setup(b => b.BuildTodotixRequest(It.IsAny<Guid>(), "a@b.com", template, "America/La_Paz", It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>())).Returns(todotixRequest);
        creationBuilder.Setup(b => b.BuildOutboxEvent(It.IsAny<Guid>(), tenantId, todotixRequest)).Returns(todotixEvent);
        creationBuilder.Setup(b => b.BuildExpirationOutboxEvent(It.IsAny<Guid>(), tenantId, studentId, It.IsAny<DateTime>())).Returns(expirationEvent);
        creationBuilder.Setup(b => b.BuildPendingDebtDto(It.IsAny<Guid>())).Returns(pendingDto);

        pendingDao.Setup(d => d.CreateAsync(pending, transactionScope.Object)).Returns(Task.CompletedTask);
        todotixOutboxDao.Setup(d => d.InsertAsync(todotixEvent, transactionScope.Object)).Returns(Task.CompletedTask);
        expirationOutboxDao.Setup(d => d.InsertAsync(expirationEvent, transactionScope.Object)).Returns(Task.CompletedTask);

        CreateQrDebtOutcome outcome = await sut.CreateDebtAsync(templateId, "a@b.com", new CreateQrDebtDto());

        Assert.That(outcome, Is.TypeOf<CreateQrDebtOutcome.Success>());
        transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }
}
