using Backend.Application.Commands;
using Backend.Application.Handlers;
using Backend.Application.Results;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Dtos.External.Todotix;
using Backend.Dtos.QrPayments.Output;
using Backend.Entities.Subscriptions;
using Backend.Entities.Todotix;
using Backend.Options;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.Extensions.Options;

using Moq;

using Test.Infrastructure;

namespace Test.Application.Handlers;

[TestFixture]
public class CreateSubscriptionQrDebtCommandHandlerTests
{
    private Mock<ISubscriptionPlanDao> planDao = null!;
    private Mock<IPendingSubscriptionPaymentDao> pendingDao = null!;
    private Mock<ITodotixOutboxDao> todotixOutboxDao = null!;
    private Mock<IUnitOfWork> unitOfWork = null!;
    private Mock<ITransactionScope> transactionScope = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<ISubscriptionCreationBuilder> creationBuilder = null!;
    private CreateSubscriptionQrDebtCommandHandler sut = null!;
    private Guid tenantId;

    [SetUp]
    public void Setup()
    {
        planDao = new Mock<ISubscriptionPlanDao>(MockBehavior.Strict);
        pendingDao = new Mock<IPendingSubscriptionPaymentDao>(MockBehavior.Strict);
        todotixOutboxDao = new Mock<ITodotixOutboxDao>(MockBehavior.Strict);
        (unitOfWork, transactionScope) = UnitOfWorkMockHelper.BuildCommittingMocks();
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        creationBuilder = new Mock<ISubscriptionCreationBuilder>(MockBehavior.Strict);

        tenantId = Guid.NewGuid();
        claimContext.Setup(c => c.TenantId).Returns(tenantId);
        claimContext.Setup(c => c.TenantTimezone).Returns("America/La_Paz");
        claimContext.Setup(c => c.TenantName).Returns("Acme");

        sut = BuildSut("platform-key");
    }

    private CreateSubscriptionQrDebtCommandHandler BuildSut(string platformAppKey)
    {
        IOptions<TodotixOptions> options = Options.Create(new TodotixOptions { PlatformAppKey = platformAppKey });
        return new CreateSubscriptionQrDebtCommandHandler(
            planDao.Object,
            pendingDao.Object,
            todotixOutboxDao.Object,
            unitOfWork.Object,
            claimContext.Object,
            creationBuilder.Object,
            options);
    }

    [Test]
    public async Task Handle_WhenPlanMissing_ReturnsPlanNotFound()
    {
        planDao.Setup(d => d.GetByLevelAsync(2)).ReturnsAsync((SubscriptionPlan?)null);

        CreateSubscriptionDebtOutcome outcome = await sut.Handle(new CreateSubscriptionQrDebtCommand(2, "a@b.com"));

        Assert.That(outcome, Is.TypeOf<CreateSubscriptionDebtOutcome.PlanNotFound>());
    }

    [Test]
    public async Task Handle_WhenActiveSubscriptionDebtExists_ReturnsActiveSubscriptionDebt()
    {
        planDao.Setup(d => d.GetByLevelAsync(2)).ReturnsAsync(new SubscriptionPlan { Level = 2, Price = 180 });
        pendingDao.Setup(d => d.CountActiveForTenantAsync(tenantId, It.IsAny<DateTime>())).ReturnsAsync(1);

        CreateSubscriptionDebtOutcome outcome = await sut.Handle(new CreateSubscriptionQrDebtCommand(2, "a@b.com"));

        Assert.That(outcome, Is.TypeOf<CreateSubscriptionDebtOutcome.ActiveSubscriptionDebt>());
    }

    [Test]
    public async Task Handle_WhenPlatformAppKeyMissing_ReturnsPaymentNotConfigured()
    {
        sut = BuildSut(string.Empty);
        planDao.Setup(d => d.GetByLevelAsync(2)).ReturnsAsync(new SubscriptionPlan { Level = 2, Price = 180 });
        pendingDao.Setup(d => d.CountActiveForTenantAsync(tenantId, It.IsAny<DateTime>())).ReturnsAsync(0);

        CreateSubscriptionDebtOutcome outcome = await sut.Handle(new CreateSubscriptionQrDebtCommand(2, "a@b.com"));

        Assert.That(outcome, Is.TypeOf<CreateSubscriptionDebtOutcome.PaymentNotConfigured>());
    }

    [Test]
    public async Task Handle_HappyPath_PersistsPendingAndOutboxAndReturnsSuccess()
    {
        SubscriptionPlan plan = new() { Level = 2, Price = 180, DurationAmount = 1, DurationUnit = "Month" };
        planDao.Setup(d => d.GetByLevelAsync(2)).ReturnsAsync(plan);
        pendingDao.Setup(d => d.CountActiveForTenantAsync(tenantId, It.IsAny<DateTime>())).ReturnsAsync(0);

        PendingSubscriptionPayment pending = new() { Id = Guid.NewGuid(), TenantId = tenantId, Level = 2, Cost = 180 };
        RegisterDebtRequest todotixRequest = new();
        TodotixOutboxEvent outboxEvent = new() { Id = pending.Id, PendingId = pending.Id, TenantId = tenantId };
        QrDebtPendingDto pendingDto = new() { IdentificadorDeuda = pending.Id, Status = "Pending" };

        creationBuilder.Setup(b => b.BuildPendingPayment(It.IsAny<Guid>(), tenantId, plan, It.IsAny<DateTime>())).Returns(pending);
        creationBuilder.Setup(b => b.BuildTodotixRequest(It.IsAny<Guid>(), "a@b.com", plan, "America/La_Paz", It.IsAny<string>(), It.IsAny<DateTime>(), "platform-key")).Returns(todotixRequest);
        creationBuilder.Setup(b => b.BuildOutboxEvent(It.IsAny<Guid>(), tenantId, todotixRequest)).Returns(outboxEvent);
        creationBuilder.Setup(b => b.BuildPendingDebtDto(It.IsAny<Guid>())).Returns(pendingDto);
        pendingDao.Setup(d => d.CreateAsync(pending, transactionScope.Object)).Returns(Task.CompletedTask);
        todotixOutboxDao.Setup(d => d.InsertAsync(outboxEvent, transactionScope.Object)).Returns(Task.CompletedTask);

        CreateSubscriptionDebtOutcome outcome = await sut.Handle(new CreateSubscriptionQrDebtCommand(2, "a@b.com"));

        Assert.That(outcome, Is.TypeOf<CreateSubscriptionDebtOutcome.Success>());
        transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }
}
