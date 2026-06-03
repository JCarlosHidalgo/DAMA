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
    private Mock<ISubscriptionPlanDao> _planDao = null!;
    private Mock<IPendingSubscriptionPaymentDao> _pendingDao = null!;
    private Mock<ITodotixOutboxDao> _todotixOutboxDao = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ITransactionScope> _transactionScope = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<ISubscriptionCreationBuilder> _creationBuilder = null!;
    private CreateSubscriptionQrDebtCommandHandler _sut = null!;
    private Guid _tenantId;

    [SetUp]
    public void Setup()
    {
        _planDao = new Mock<ISubscriptionPlanDao>(MockBehavior.Strict);
        _pendingDao = new Mock<IPendingSubscriptionPaymentDao>(MockBehavior.Strict);
        _todotixOutboxDao = new Mock<ITodotixOutboxDao>(MockBehavior.Strict);
        (_unitOfWork, _transactionScope) = UnitOfWorkMockHelper.BuildCommittingMocks();
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _creationBuilder = new Mock<ISubscriptionCreationBuilder>(MockBehavior.Strict);

        _tenantId = Guid.NewGuid();
        _claimContext.Setup(c => c.TenantId).Returns(_tenantId);
        _claimContext.Setup(c => c.TenantTimezone).Returns("America/La_Paz");
        _claimContext.Setup(c => c.TenantName).Returns("Acme");

        _sut = BuildSut("platform-key");
    }

    private CreateSubscriptionQrDebtCommandHandler BuildSut(string platformAppKey)
    {
        IOptions<TodotixOptions> options = Options.Create(new TodotixOptions { PlatformAppKey = platformAppKey });
        return new CreateSubscriptionQrDebtCommandHandler(
            _planDao.Object,
            _pendingDao.Object,
            _todotixOutboxDao.Object,
            _unitOfWork.Object,
            _claimContext.Object,
            _creationBuilder.Object,
            options);
    }

    [Test]
    public async Task Handle_WhenPlanMissing_ReturnsPlanNotFound()
    {
        _planDao.Setup(d => d.GetByLevelAsync(2)).ReturnsAsync((SubscriptionPlan?)null);

        CreateSubscriptionDebtOutcome outcome = await _sut.Handle(new CreateSubscriptionQrDebtCommand(2, "a@b.com"));

        Assert.That(outcome, Is.TypeOf<CreateSubscriptionDebtOutcome.PlanNotFound>());
    }

    [Test]
    public async Task Handle_WhenActiveSubscriptionDebtExists_ReturnsSuccessWithAlreadyGenerated()
    {
        var existingDebtId = Guid.NewGuid();
        _planDao.Setup(d => d.GetByLevelAsync(2)).ReturnsAsync(new SubscriptionPlan { Level = 2, Price = 180 });
        _pendingDao.Setup(d => d.GetActiveForTenantAsync(_tenantId, It.IsAny<DateTime>())).ReturnsAsync(existingDebtId);
        _creationBuilder.Setup(b => b.BuildPendingDebtDto(existingDebtId, true))
            .Returns(new QrDebtPendingDto { IdentificadorDeuda = existingDebtId, Status = "Pending", AlreadyGenerated = true });

        CreateSubscriptionDebtOutcome outcome = await _sut.Handle(new CreateSubscriptionQrDebtCommand(2, "a@b.com"));

        Assert.That(outcome, Is.TypeOf<CreateSubscriptionDebtOutcome.Success>());
        var success = (CreateSubscriptionDebtOutcome.Success)outcome;
        Assert.Multiple(() =>
        {
            Assert.That(success.Created.AlreadyGenerated, Is.True);
            Assert.That(success.Created.IdentificadorDeuda, Is.EqualTo(existingDebtId));
        });
    }

    [Test]
    public async Task Handle_WhenPlatformAppKeyMissing_ReturnsPaymentNotConfigured()
    {
        _sut = BuildSut(string.Empty);
        _planDao.Setup(d => d.GetByLevelAsync(2)).ReturnsAsync(new SubscriptionPlan { Level = 2, Price = 180 });
        _pendingDao.Setup(d => d.GetActiveForTenantAsync(_tenantId, It.IsAny<DateTime>())).ReturnsAsync((Guid?)null);

        CreateSubscriptionDebtOutcome outcome = await _sut.Handle(new CreateSubscriptionQrDebtCommand(2, "a@b.com"));

        Assert.That(outcome, Is.TypeOf<CreateSubscriptionDebtOutcome.PaymentNotConfigured>());
    }

    [Test]
    public async Task Handle_HappyPath_PersistsPendingAndOutboxAndReturnsSuccess()
    {
        SubscriptionPlan plan = new() { Level = 2, Price = 180, DurationAmount = 1, DurationUnit = "Month" };
        _planDao.Setup(d => d.GetByLevelAsync(2)).ReturnsAsync(plan);
        _pendingDao.Setup(d => d.GetActiveForTenantAsync(_tenantId, It.IsAny<DateTime>())).ReturnsAsync((Guid?)null);

        PendingSubscriptionPayment pending = new() { Id = Guid.NewGuid(), TenantId = _tenantId, Level = 2, Cost = 180 };
        RegisterDebtRequest todotixRequest = new();
        TodotixOutboxEvent outboxEvent = new() { Id = pending.Id, PendingId = pending.Id, TenantId = _tenantId };
        QrDebtPendingDto pendingDto = new() { IdentificadorDeuda = pending.Id, Status = "Pending" };

        _creationBuilder.Setup(b => b.BuildPendingPayment(It.IsAny<Guid>(), _tenantId, plan, It.IsAny<DateTime>())).Returns(pending);
        _creationBuilder.Setup(b => b.BuildTodotixRequest(It.IsAny<Guid>(), "a@b.com", plan, "America/La_Paz", It.IsAny<string>(), It.IsAny<DateTime>(), "platform-key")).Returns(todotixRequest);
        _creationBuilder.Setup(b => b.BuildOutboxEvent(It.IsAny<Guid>(), _tenantId, todotixRequest)).Returns(outboxEvent);
        _creationBuilder.Setup(b => b.BuildPendingDebtDto(It.IsAny<Guid>(), false)).Returns(pendingDto);
        _pendingDao.Setup(d => d.CreateAsync(pending, _transactionScope.Object)).Returns(Task.CompletedTask);
        _todotixOutboxDao.Setup(d => d.InsertAsync(outboxEvent, _transactionScope.Object)).Returns(Task.CompletedTask);

        CreateSubscriptionDebtOutcome outcome = await _sut.Handle(new CreateSubscriptionQrDebtCommand(2, "a@b.com"));

        Assert.That(outcome, Is.TypeOf<CreateSubscriptionDebtOutcome.Success>());
        _transactionScope.Verify(s => s.CommitAsync(), Times.Once);
    }
}
