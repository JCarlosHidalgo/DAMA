using AutoMapper;

using Backend.AutoMapperProfiles;
using Backend.Builders;
using Backend.Claims;
using Backend.Common;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Dtos.QrPayments.Output;
using Backend.Entities.QrPayments;
using Backend.Entities.Todotix;
using Backend.Results.QrPayments;
using Backend.Services.Concrete.QrPayments;

using Moq;

namespace Test.Services.Concrete.QrPayments;

[TestFixture]
public class QrPaymentQueryServiceTests
{
    private Mock<IPendingQrPaymentDao> _pendingDao = null!;
    private Mock<ISuccessQrPaymentDao> _successDao = null!;
    private Mock<IFailedQrPaymentDao> _failedDao = null!;
    private Mock<ITodotixOutboxDao> _todotixOutboxDao = null!;
    private IMapper _autoMapper = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private QrPaymentViewBuilder _viewBuilder = null!;
    private QrPaymentQueryService _sut = null!;
    private Guid _tenantId;
    private Guid _studentId;

    [SetUp]
    public void Setup()
    {
        _pendingDao = new Mock<IPendingQrPaymentDao>(MockBehavior.Strict);
        _successDao = new Mock<ISuccessQrPaymentDao>(MockBehavior.Strict);
        _failedDao = new Mock<IFailedQrPaymentDao>(MockBehavior.Strict);
        _todotixOutboxDao = new Mock<ITodotixOutboxDao>(MockBehavior.Strict);

        var mapperConfiguration = new MapperConfiguration(
            configuration => configuration.AddProfile<DebtTemplateProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _autoMapper = mapperConfiguration.CreateMapper();

        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _tenantId = Guid.NewGuid();
        _studentId = Guid.NewGuid();
        _claimContext.Setup(c => c.TenantId).Returns(_tenantId);
        _claimContext.Setup(c => c.UserId).Returns(_studentId);

        _viewBuilder = new QrPaymentViewBuilder();

        _sut = new QrPaymentQueryService(
            _pendingDao.Object,
            _successDao.Object,
            _failedDao.Object,
            _todotixOutboxDao.Object,
            _autoMapper,
            _claimContext.Object,
            _viewBuilder);
    }

    [Test]
    public async Task GetDebtStatusAsync_PendingWithQrUrl_ReturnsReady()
    {
        var paymentId = Guid.NewGuid();
        var pending = new PendingQrPayment { Id = paymentId, TenantId = _tenantId, QrImageUrl = "http://q" };
        _pendingDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, paymentId)).ReturnsAsync(pending);

        GetQrDebtStatusOutcome outcome = await _sut.GetDebtStatusAsync(paymentId);

        var found = (GetQrDebtStatusOutcome.Found)outcome;
        Assert.Multiple(() =>
        {
            Assert.That(found.Status.Status, Is.EqualTo("Ready"));
            Assert.That(found.Status.QrSimpleUrl, Is.EqualTo("http://q"));
        });
    }

    [Test]
    public async Task GetDebtStatusAsync_PendingNoQrAndOutboxFailed_ReturnsFailedStatus()
    {
        var paymentId = Guid.NewGuid();
        var pending = new PendingQrPayment { Id = paymentId, TenantId = _tenantId, QrImageUrl = null };
        _pendingDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, paymentId)).ReturnsAsync(pending);
        _todotixOutboxDao.Setup(d => d.GetByPendingIdAsync(paymentId)).ReturnsAsync(new TodotixOutboxEvent { Status = "Failed", LastError = "todotix down" });

        GetQrDebtStatusOutcome outcome = await _sut.GetDebtStatusAsync(paymentId);

        var found = (GetQrDebtStatusOutcome.Found)outcome;
        Assert.Multiple(() =>
        {
            Assert.That(found.Status.Status, Is.EqualTo("Failed"));
            Assert.That(found.Status.Error, Is.EqualTo("todotix down"));
        });
    }

    [Test]
    public async Task GetDebtStatusAsync_PendingNoQrAndOutboxNotFailed_ReturnsPending()
    {
        var paymentId = Guid.NewGuid();
        var pending = new PendingQrPayment { Id = paymentId, TenantId = _tenantId, QrImageUrl = null };
        _pendingDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, paymentId)).ReturnsAsync(pending);
        _todotixOutboxDao.Setup(d => d.GetByPendingIdAsync(paymentId)).ReturnsAsync((TodotixOutboxEvent?)null);

        GetQrDebtStatusOutcome outcome = await _sut.GetDebtStatusAsync(paymentId);

        var found = (GetQrDebtStatusOutcome.Found)outcome;
        Assert.That(found.Status.Status, Is.EqualTo("Pending"));
    }

    [Test]
    public async Task GetDebtStatusAsync_SuccessExistsForTenant_ReturnsReady()
    {
        var paymentId = Guid.NewGuid();
        _pendingDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, paymentId)).ReturnsAsync((PendingQrPayment?)null);
        _successDao.Setup(d => d.GetByIdAsync(paymentId)).ReturnsAsync(new SuccessQrPayment { Id = paymentId, TenantId = _tenantId });

        GetQrDebtStatusOutcome outcome = await _sut.GetDebtStatusAsync(paymentId);

        var found = (GetQrDebtStatusOutcome.Found)outcome;
        Assert.That(found.Status.Status, Is.EqualTo("Ready"));
    }

    [Test]
    public async Task GetDebtStatusAsync_SuccessForOtherTenant_ReturnsNotFound()
    {
        var paymentId = Guid.NewGuid();
        _pendingDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, paymentId)).ReturnsAsync((PendingQrPayment?)null);
        _successDao.Setup(d => d.GetByIdAsync(paymentId)).ReturnsAsync(new SuccessQrPayment { Id = paymentId, TenantId = Guid.NewGuid() });

        GetQrDebtStatusOutcome outcome = await _sut.GetDebtStatusAsync(paymentId);

        Assert.That(outcome, Is.TypeOf<GetQrDebtStatusOutcome.NotFound>());
    }

    [Test]
    public async Task GetDebtStatusAsync_Missing_ReturnsNotFound()
    {
        var paymentId = Guid.NewGuid();
        _pendingDao.Setup(d => d.GetByIdForTenantAsync(_tenantId, paymentId)).ReturnsAsync((PendingQrPayment?)null);
        _successDao.Setup(d => d.GetByIdAsync(paymentId)).ReturnsAsync((SuccessQrPayment?)null);

        GetQrDebtStatusOutcome outcome = await _sut.GetDebtStatusAsync(paymentId);

        Assert.That(outcome, Is.TypeOf<GetQrDebtStatusOutcome.NotFound>());
    }

    [Test]
    public async Task ListPendingAsync_WithItems_FetchesPageAndComputesIndices()
    {
        _pendingDao.Setup(d => d.CountByStudentForTenantAsync(_tenantId, _studentId)).ReturnsAsync(25);
        _pendingDao.Setup(d => d.GetPageByStudentForTenantAsync(_tenantId, _studentId, 10, 10))
                  .ReturnsAsync([new() { Id = Guid.NewGuid(), ClassQuantity = 1, Cost = 100 }]);

        PageDto<PendingQrDebtDto> page = await _sut.ListPendingAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(page.CurrentIndex, Is.EqualTo(1));
            Assert.That(page.MaxIndex, Is.EqualTo(2));
            Assert.That(page.Items, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task ListPendingAsync_PageBeyondMax_ReturnsEmpty()
    {
        _pendingDao.Setup(d => d.CountByStudentForTenantAsync(_tenantId, _studentId)).ReturnsAsync(0);

        PageDto<PendingQrDebtDto> page = await _sut.ListPendingAsync(5);

        Assert.Multiple(() =>
        {
            Assert.That(page.CurrentIndex, Is.EqualTo(5));
            Assert.That(page.MaxIndex, Is.EqualTo(0));
            Assert.That(page.Items, Is.Empty);
        });
    }

    [Test]
    public async Task ListSuccessAsync_FetchesPageFromSuccessDao()
    {
        _successDao.Setup(d => d.CountByStudentForTenantAsync(_tenantId, _studentId)).ReturnsAsync(3);
        _successDao.Setup(d => d.GetPageByStudentForTenantAsync(_tenantId, _studentId, 0, 10))
                  .ReturnsAsync([new() { Id = Guid.NewGuid(), ClassQuantity = 1, Cost = 10 }]);

        PageDto<SuccessQrPaymentDto> page = await _sut.ListSuccessAsync(0);

        Assert.That(page.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ListFailedAsync_FetchesPageFromFailedDao()
    {
        _failedDao.Setup(d => d.CountByStudentForTenantAsync(_tenantId, _studentId)).ReturnsAsync(3);
        _failedDao.Setup(d => d.GetPageByStudentForTenantAsync(_tenantId, _studentId, 0, 10))
                 .ReturnsAsync([new() { Id = Guid.NewGuid(), ClassQuantity = 1, Cost = 10 }]);

        PageDto<FailedQrPaymentDto> page = await _sut.ListFailedAsync(0);

        Assert.That(page.Items, Has.Count.EqualTo(1));
    }
}
