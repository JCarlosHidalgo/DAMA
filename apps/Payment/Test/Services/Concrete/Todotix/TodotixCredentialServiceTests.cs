using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.PaymentCredentials;
using Backend.Dtos.External.Todotix;
using Backend.Dtos.Todotix.Input;
using Backend.Dtos.Todotix.Output;
using Backend.Entities.PaymentCredentials;
using Backend.Results.Todotix;
using Backend.Security;
using Backend.Services.Abstract.Todotix;
using Backend.Services.Concrete.Todotix;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Test.Services.Concrete.Todotix;

[TestFixture]
public class TodotixCredentialServiceTests
{
    private Mock<ITenantPaymentCredentialReader> _credentialReader = null!;
    private Mock<ITenantPaymentCredentialWriter> _credentialWriter = null!;
    private Mock<IAppKeyCipher> _appKeyCipher = null!;
    private Mock<IClaimContext> _claimContext = null!;
    private Mock<ITodotixClient> _todotixClient = null!;
    private Mock<ITodotixCredentialTestBuilder> _testBuilder = null!;
    private TodotixCredentialService _sut = null!;
    private Guid _tenantId;

    [SetUp]
    public void Setup()
    {
        _credentialReader = new Mock<ITenantPaymentCredentialReader>(MockBehavior.Strict);
        _credentialWriter = new Mock<ITenantPaymentCredentialWriter>(MockBehavior.Strict);
        _appKeyCipher = new Mock<IAppKeyCipher>(MockBehavior.Strict);
        _claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        _todotixClient = new Mock<ITodotixClient>(MockBehavior.Strict);
        _testBuilder = new Mock<ITodotixCredentialTestBuilder>(MockBehavior.Strict);
        _tenantId = Guid.NewGuid();
        _claimContext.Setup(c => c.TenantId).Returns(_tenantId);

        _sut = new TodotixCredentialService(
            _credentialReader.Object,
            _credentialWriter.Object,
            _appKeyCipher.Object,
            _claimContext.Object,
            new TodotixCredentialViewBuilder(),
            _todotixClient.Object,
            _testBuilder.Object,
            NullLogger<TodotixCredentialService>.Instance);
    }

    [Test]
    public async Task GetStatusAsync_WhenCustomKeyExists_ReportsConfiguredAndMasksStoredKey()
    {
        var credential = new TenantPaymentCredential
        {
            Id = _tenantId,
            TodotixAppKey = "cipher"
        };
        _credentialReader.Setup(r => r.GetByTenantAsync(_tenantId)).ReturnsAsync(credential);
        _appKeyCipher.Setup(c => c.Decrypt("cipher")).Returns("51599bd3-eed3-2826-45a4-a16c2fcc2724");

        TodotixAppKeyStatusDto status = await _sut.GetStatusAsync();

        Assert.Multiple(() =>
        {
            Assert.That(status.HasCustomKey, Is.True);
            Assert.That(status.MaskedAppKey, Does.EndWith("2724"));
            Assert.That(status.MaskedAppKey, Does.StartWith("•"));
        });
    }

    [Test]
    public async Task GetStatusAsync_WhenNoCustomKey_ReportsNotConfiguredWithoutMaskedKey()
    {
        _credentialReader.Setup(r => r.GetByTenantAsync(_tenantId)).ReturnsAsync((TenantPaymentCredential?)null);

        TodotixAppKeyStatusDto status = await _sut.GetStatusAsync();

        Assert.Multiple(() =>
        {
            Assert.That(status.HasCustomKey, Is.False);
            Assert.That(status.MaskedAppKey, Is.Null);
        });
    }

    [Test]
    public async Task GetAvailabilityAsync_WhenCredentialExists_ReportsAvailable()
    {
        var credential = new TenantPaymentCredential
        {
            Id = _tenantId,
            TodotixAppKey = "cipher"
        };
        _credentialReader.Setup(r => r.GetByTenantAsync(_tenantId)).ReturnsAsync(credential);

        PaymentAvailabilityDto availability = await _sut.GetAvailabilityAsync();

        Assert.That(availability.HasPaymentCredentials, Is.True);
    }

    [Test]
    public async Task GetAvailabilityAsync_WhenNoCredential_ReportsUnavailable()
    {
        _credentialReader.Setup(r => r.GetByTenantAsync(_tenantId)).ReturnsAsync((TenantPaymentCredential?)null);

        PaymentAvailabilityDto availability = await _sut.GetAvailabilityAsync();

        Assert.That(availability.HasPaymentCredentials, Is.False);
    }

    [Test]
    public async Task RevealAsync_WhenConfigured_ReturnsDecryptedStoredKey()
    {
        var credential = new TenantPaymentCredential
        {
            Id = _tenantId,
            TodotixAppKey = "cipher"
        };
        _credentialReader.Setup(r => r.GetByTenantAsync(_tenantId)).ReturnsAsync(credential);
        _appKeyCipher.Setup(c => c.Decrypt("cipher")).Returns("51599bd3-eed3-2826-45a4-a16c2fcc2724");

        TodotixAppKeyRevealDto reveal = await _sut.RevealAsync();

        Assert.That(reveal.AppKey, Is.EqualTo("51599bd3-eed3-2826-45a4-a16c2fcc2724"));
    }

    [Test]
    public async Task RevealAsync_WhenNotConfigured_ReturnsEmpty()
    {
        _credentialReader.Setup(r => r.GetByTenantAsync(_tenantId)).ReturnsAsync((TenantPaymentCredential?)null);

        TodotixAppKeyRevealDto reveal = await _sut.RevealAsync();

        Assert.That(reveal.AppKey, Is.Empty);
    }

    [Test]
    public async Task UpdateAsync_EncryptsAndUpsertsForTenant()
    {
        _appKeyCipher.Setup(c => c.Encrypt("51599bd3-eed3-2826-45a4-a16c2fcc2724")).Returns("cipher");
        _credentialWriter.Setup(w => w.UpsertAsync(_tenantId, "cipher")).Returns(Task.CompletedTask);

        UpdateTodotixAppKeyOutcome outcome = await _sut.UpdateAsync(
            new UpdateTodotixAppKeyDto { AppKey = "51599bd3-eed3-2826-45a4-a16c2fcc2724" });

        Assert.That(outcome, Is.TypeOf<UpdateTodotixAppKeyOutcome.Updated>());
        _credentialWriter.Verify(w => w.UpsertAsync(_tenantId, "cipher"), Times.Once);
    }

    [Test]
    public async Task TestAsync_WhenNoCredentialConfigured_ReportsNotConfigured()
    {
        _credentialReader.Setup(r => r.GetByTenantAsync(_tenantId)).ReturnsAsync((TenantPaymentCredential?)null);

        TestTodotixCredentialOutcome outcome = await _sut.TestAsync();

        Assert.That(outcome, Is.TypeOf<TestTodotixCredentialOutcome.NotConfigured>());
    }

    [Test]
    public async Task TestAsync_WhenTodotixRegistersDebt_ReportsWorks()
    {
        ArrangeConfiguredCredential();
        _todotixClient
            .Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()))
            .ReturnsAsync(new RegisterDebtResponse { Error = 0, QrSimpleUrl = "https://qr" });

        TestTodotixCredentialOutcome outcome = await _sut.TestAsync();

        Assert.That(outcome, Is.TypeOf<TestTodotixCredentialOutcome.Works>());
    }

    [Test]
    public async Task TestAsync_WhenTodotixReturnsError_ReportsFailed()
    {
        ArrangeConfiguredCredential();
        _todotixClient
            .Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()))
            .ReturnsAsync(new RegisterDebtResponse { Error = 1, QrSimpleUrl = null });

        TestTodotixCredentialOutcome outcome = await _sut.TestAsync();

        Assert.That(outcome, Is.TypeOf<TestTodotixCredentialOutcome.Failed>());
    }

    [Test]
    public async Task TestAsync_WhenDebtRegistersWithoutQrUrl_ReportsWorks()
    {
        ArrangeConfiguredCredential();
        _todotixClient
            .Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()))
            .ReturnsAsync(new RegisterDebtResponse { Error = 0, QrSimpleUrl = null });

        TestTodotixCredentialOutcome outcome = await _sut.TestAsync();

        Assert.That(outcome, Is.TypeOf<TestTodotixCredentialOutcome.Works>());
    }

    [Test]
    public async Task TestAsync_WhenTodotixCallThrows_ReportsFailed()
    {
        ArrangeConfiguredCredential();
        _todotixClient
            .Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()))
            .ThrowsAsync(new HttpRequestException("unauthorized"));

        TestTodotixCredentialOutcome outcome = await _sut.TestAsync();

        Assert.That(outcome, Is.TypeOf<TestTodotixCredentialOutcome.Failed>());
    }

    private void ArrangeConfiguredCredential()
    {
        var credential = new TenantPaymentCredential
        {
            Id = _tenantId,
            TodotixAppKey = "cipher"
        };
        _credentialReader.Setup(r => r.GetByTenantAsync(_tenantId)).ReturnsAsync(credential);
        _appKeyCipher.Setup(c => c.Decrypt("cipher")).Returns("51599bd3-eed3-2826-45a4-a16c2fcc2724");
        _claimContext.Setup(c => c.TenantTimezone).Returns("America/La_Paz");
        _testBuilder
            .Setup(b => b.BuildCredentialTestRequest("51599bd3-eed3-2826-45a4-a16c2fcc2724", "America/La_Paz"))
            .Returns(new RegisterDebtRequest());
    }
}
