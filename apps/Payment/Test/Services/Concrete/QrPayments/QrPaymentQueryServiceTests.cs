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
    private Mock<IPendingQrPaymentDao> pendingDao = null!;
    private Mock<ISuccessQrPaymentDao> successDao = null!;
    private Mock<IFailedQrPaymentDao> failedDao = null!;
    private Mock<ITodotixOutboxDao> todotixOutboxDao = null!;
    private IMapper autoMapper = null!;
    private Mock<IClaimContext> claimContext = null!;
    private QrPaymentViewBuilder viewBuilder = null!;
    private QrPaymentQueryService sut = null!;
    private Guid tenantId;
    private Guid studentId;

    [SetUp]
    public void Setup()
    {
        pendingDao = new Mock<IPendingQrPaymentDao>(MockBehavior.Strict);
        successDao = new Mock<ISuccessQrPaymentDao>(MockBehavior.Strict);
        failedDao = new Mock<IFailedQrPaymentDao>(MockBehavior.Strict);
        todotixOutboxDao = new Mock<ITodotixOutboxDao>(MockBehavior.Strict);

        var mapperConfiguration = new MapperConfiguration(
            configuration => configuration.AddProfile<DebtTemplateProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        autoMapper = mapperConfiguration.CreateMapper();

        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        tenantId = Guid.NewGuid();
        studentId = Guid.NewGuid();
        claimContext.Setup(c => c.TenantId).Returns(tenantId);
        claimContext.Setup(c => c.UserId).Returns(studentId);

        viewBuilder = new QrPaymentViewBuilder();

        sut = new QrPaymentQueryService(
            pendingDao.Object,
            successDao.Object,
            failedDao.Object,
            todotixOutboxDao.Object,
            autoMapper,
            claimContext.Object,
            viewBuilder);
    }

    [Test]
    public async Task GetDebtStatusAsync_PendingWithQrUrl_ReturnsReady()
    {
        var paymentId = Guid.NewGuid();
        var pending = new PendingQrPayment { Id = paymentId, TenantId = tenantId, QrImageUrl = "http://q" };
        pendingDao.Setup(d => d.GetByIdForTenantAsync(tenantId, paymentId)).ReturnsAsync(pending);

        GetQrDebtStatusOutcome outcome = await sut.GetDebtStatusAsync(paymentId);

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
        var pending = new PendingQrPayment { Id = paymentId, TenantId = tenantId, QrImageUrl = null };
        pendingDao.Setup(d => d.GetByIdForTenantAsync(tenantId, paymentId)).ReturnsAsync(pending);
        todotixOutboxDao.Setup(d => d.GetByPendingIdAsync(paymentId)).ReturnsAsync(new TodotixOutboxEvent { Status = "Failed", LastError = "todotix down" });

        GetQrDebtStatusOutcome outcome = await sut.GetDebtStatusAsync(paymentId);

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
        var pending = new PendingQrPayment { Id = paymentId, TenantId = tenantId, QrImageUrl = null };
        pendingDao.Setup(d => d.GetByIdForTenantAsync(tenantId, paymentId)).ReturnsAsync(pending);
        todotixOutboxDao.Setup(d => d.GetByPendingIdAsync(paymentId)).ReturnsAsync((TodotixOutboxEvent?)null);

        GetQrDebtStatusOutcome outcome = await sut.GetDebtStatusAsync(paymentId);

        var found = (GetQrDebtStatusOutcome.Found)outcome;
        Assert.That(found.Status.Status, Is.EqualTo("Pending"));
    }

    [Test]
    public async Task GetDebtStatusAsync_SuccessExistsForTenant_ReturnsReady()
    {
        var paymentId = Guid.NewGuid();
        pendingDao.Setup(d => d.GetByIdForTenantAsync(tenantId, paymentId)).ReturnsAsync((PendingQrPayment?)null);
        successDao.Setup(d => d.GetByIdAsync(paymentId)).ReturnsAsync(new SuccessQrPayment { Id = paymentId, TenantId = tenantId });

        GetQrDebtStatusOutcome outcome = await sut.GetDebtStatusAsync(paymentId);

        var found = (GetQrDebtStatusOutcome.Found)outcome;
        Assert.That(found.Status.Status, Is.EqualTo("Ready"));
    }

    [Test]
    public async Task GetDebtStatusAsync_SuccessForOtherTenant_ReturnsNotFound()
    {
        var paymentId = Guid.NewGuid();
        pendingDao.Setup(d => d.GetByIdForTenantAsync(tenantId, paymentId)).ReturnsAsync((PendingQrPayment?)null);
        successDao.Setup(d => d.GetByIdAsync(paymentId)).ReturnsAsync(new SuccessQrPayment { Id = paymentId, TenantId = Guid.NewGuid() });

        GetQrDebtStatusOutcome outcome = await sut.GetDebtStatusAsync(paymentId);

        Assert.That(outcome, Is.TypeOf<GetQrDebtStatusOutcome.NotFound>());
    }

    [Test]
    public async Task GetDebtStatusAsync_Missing_ReturnsNotFound()
    {
        var paymentId = Guid.NewGuid();
        pendingDao.Setup(d => d.GetByIdForTenantAsync(tenantId, paymentId)).ReturnsAsync((PendingQrPayment?)null);
        successDao.Setup(d => d.GetByIdAsync(paymentId)).ReturnsAsync((SuccessQrPayment?)null);

        GetQrDebtStatusOutcome outcome = await sut.GetDebtStatusAsync(paymentId);

        Assert.That(outcome, Is.TypeOf<GetQrDebtStatusOutcome.NotFound>());
    }

    [Test]
    public async Task ListPendingAsync_WithItems_FetchesPageAndComputesIndices()
    {
        pendingDao.Setup(d => d.CountByStudentForTenantAsync(tenantId, studentId)).ReturnsAsync(25);
        pendingDao.Setup(d => d.GetPageByStudentForTenantAsync(tenantId, studentId, 10, 10))
                  .ReturnsAsync([new() { Id = Guid.NewGuid(), ClassQuantity = 1, Cost = 100 }]);

        PageDto<PendingQrDebtDto> page = await sut.ListPendingAsync(1);

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
        pendingDao.Setup(d => d.CountByStudentForTenantAsync(tenantId, studentId)).ReturnsAsync(0);

        PageDto<PendingQrDebtDto> page = await sut.ListPendingAsync(5);

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
        successDao.Setup(d => d.CountByStudentForTenantAsync(tenantId, studentId)).ReturnsAsync(3);
        successDao.Setup(d => d.GetPageByStudentForTenantAsync(tenantId, studentId, 0, 10))
                  .ReturnsAsync([new() { Id = Guid.NewGuid(), ClassQuantity = 1, Cost = 10 }]);

        PageDto<SuccessQrPaymentDto> page = await sut.ListSuccessAsync(0);

        Assert.That(page.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ListFailedAsync_FetchesPageFromFailedDao()
    {
        failedDao.Setup(d => d.CountByStudentForTenantAsync(tenantId, studentId)).ReturnsAsync(3);
        failedDao.Setup(d => d.GetPageByStudentForTenantAsync(tenantId, studentId, 0, 10))
                 .ReturnsAsync([new() { Id = Guid.NewGuid(), ClassQuantity = 1, Cost = 10 }]);

        PageDto<FailedQrPaymentDto> page = await sut.ListFailedAsync(0);

        Assert.That(page.Items, Has.Count.EqualTo(1));
    }
}
