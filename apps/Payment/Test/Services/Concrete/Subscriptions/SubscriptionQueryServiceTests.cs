using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Entities.Subscriptions;
using Backend.Results.QrPayments;
using Backend.Services.Concrete.Subscriptions;

using Moq;

namespace Test.Services.Concrete.Subscriptions;

[TestFixture]
public class SubscriptionQueryServiceTests
{
    private Mock<IPendingSubscriptionPaymentDao> _pendingDao = null!;
    private Mock<ISuccessSubscriptionPaymentDao> _successDao = null!;
    private Mock<ITodotixOutboxDao> _todotixOutboxDao = null!;
    private Mock<ISubscriptionPlanDao> _subscriptionPlanDao = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private QrPaymentViewBuilder _viewBuilder = null!;
    private SubscriptionQueryService _sut = null!;
    private Guid _tenantId;

    [SetUp]
    public void Setup()
    {
        _pendingDao = new Mock<IPendingSubscriptionPaymentDao>(MockBehavior.Strict);
        _successDao = new Mock<ISuccessSubscriptionPaymentDao>(MockBehavior.Strict);
        _todotixOutboxDao = new Mock<ITodotixOutboxDao>(MockBehavior.Strict);
        _subscriptionPlanDao = new Mock<ISubscriptionPlanDao>(MockBehavior.Strict);

        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _tenantId = Guid.NewGuid();
        _claimContext.Setup(c => c.TenantId).Returns(_tenantId);

        _viewBuilder = new QrPaymentViewBuilder();

        _sut = new SubscriptionQueryService(
            _pendingDao.Object,
            _successDao.Object,
            _todotixOutboxDao.Object,
            _subscriptionPlanDao.Object,
            _claimContext.Object,
            _viewBuilder);
    }

    [Test]
    public async Task GetDebtStatusAsync_SuccessExistsForTenant_ReturnsReady()
    {
        var paymentId = Guid.NewGuid();
        _pendingDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, paymentId)).ReturnsAsync((PendingSubscriptionPayment?)null);
        _successDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, paymentId)).ReturnsAsync(new SuccessSubscriptionPayment { Id = paymentId, TenantId = _tenantId });

        GetQrDebtStatusOutcome outcome = await _sut.GetDebtStatusAsync(paymentId);

        var found = (GetQrDebtStatusOutcome.Found)outcome;
        Assert.That(found.Status.Status, Is.EqualTo("Ready"));
    }

    [Test]
    public async Task GetDebtStatusAsync_SuccessForOtherTenant_ReturnsNotFound()
    {
        var paymentId = Guid.NewGuid();
        _pendingDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, paymentId)).ReturnsAsync((PendingSubscriptionPayment?)null);
        _successDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, paymentId)).ReturnsAsync((SuccessSubscriptionPayment?)null);

        GetQrDebtStatusOutcome outcome = await _sut.GetDebtStatusAsync(paymentId);

        Assert.That(outcome, Is.TypeOf<GetQrDebtStatusOutcome.NotFound>());
    }

    [Test]
    public async Task GetDebtStatusAsync_Missing_ReturnsNotFound()
    {
        var paymentId = Guid.NewGuid();
        _pendingDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, paymentId)).ReturnsAsync((PendingSubscriptionPayment?)null);
        _successDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, paymentId)).ReturnsAsync((SuccessSubscriptionPayment?)null);

        GetQrDebtStatusOutcome outcome = await _sut.GetDebtStatusAsync(paymentId);

        Assert.That(outcome, Is.TypeOf<GetQrDebtStatusOutcome.NotFound>());
    }
}
