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

using Moq;

namespace Test.Services.Concrete.Todotix;

[TestFixture]
public class TodotixCredentialServiceTests
{
    private Mock<ITenantPaymentCredentialReader> credentialReader = null!;
    private Mock<ITenantPaymentCredentialWriter> credentialWriter = null!;
    private Mock<IAppKeyCipher> appKeyCipher = null!;
    private Mock<ITodotixAppKeyResolver> appKeyResolver = null!;
    private Mock<IClaimContext> claimContext = null!;
    private Mock<ITodotixClient> todotixClient = null!;
    private Mock<ITodotixCredentialTestBuilder> testBuilder = null!;
    private TodotixCredentialService sut = null!;
    private Guid tenantId;

    [SetUp]
    public void Setup()
    {
        credentialReader = new Mock<ITenantPaymentCredentialReader>(MockBehavior.Strict);
        credentialWriter = new Mock<ITenantPaymentCredentialWriter>(MockBehavior.Strict);
        appKeyCipher = new Mock<IAppKeyCipher>(MockBehavior.Strict);
        appKeyResolver = new Mock<ITodotixAppKeyResolver>(MockBehavior.Strict);
        claimContext = new Mock<IClaimContext>(MockBehavior.Strict);
        todotixClient = new Mock<ITodotixClient>(MockBehavior.Strict);
        testBuilder = new Mock<ITodotixCredentialTestBuilder>(MockBehavior.Strict);
        tenantId = Guid.NewGuid();
        claimContext.Setup(c => c.TenantId).Returns(tenantId);

        sut = new TodotixCredentialService(
            credentialReader.Object,
            credentialWriter.Object,
            appKeyCipher.Object,
            appKeyResolver.Object,
            claimContext.Object,
            new TodotixCredentialViewBuilder(),
            todotixClient.Object,
            testBuilder.Object);
    }

    [Test]
    public async Task GetStatusAsync_WhenCustomKeyExists_ReportsCustomAndMasksEffectiveKey()
    {
        var credential = new TenantPaymentCredential
        {
            TenantId = tenantId,
            TodotixAppKey = "cipher"
        };
        credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync(credential);
        appKeyResolver.Setup(r => r.ResolveAsync(tenantId)).ReturnsAsync("51599bd3-eed3-2826-45a4-a16c2fcc2724");

        TodotixAppKeyStatusDto status = await sut.GetStatusAsync();

        Assert.Multiple(() =>
        {
            Assert.That(status.HasCustomKey, Is.True);
            Assert.That(status.MaskedAppKey, Does.EndWith("2724"));
            Assert.That(status.MaskedAppKey, Does.StartWith("•"));
        });
    }

    [Test]
    public async Task GetStatusAsync_WhenNoCustomKey_ReportsUsingEnvKey()
    {
        credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync((TenantPaymentCredential?)null);
        appKeyResolver.Setup(r => r.ResolveAsync(tenantId)).ReturnsAsync("env-app-key");

        TodotixAppKeyStatusDto status = await sut.GetStatusAsync();

        Assert.That(status.HasCustomKey, Is.False);
    }

    [Test]
    public async Task GetAvailabilityAsync_WhenCredentialExists_ReportsAvailable()
    {
        var credential = new TenantPaymentCredential
        {
            TenantId = tenantId,
            TodotixAppKey = "cipher"
        };
        credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync(credential);

        PaymentAvailabilityDto availability = await sut.GetAvailabilityAsync();

        Assert.That(availability.HasPaymentCredentials, Is.True);
    }

    [Test]
    public async Task GetAvailabilityAsync_WhenNoCredential_ReportsUnavailable()
    {
        credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync((TenantPaymentCredential?)null);

        PaymentAvailabilityDto availability = await sut.GetAvailabilityAsync();

        Assert.That(availability.HasPaymentCredentials, Is.False);
    }

    [Test]
    public async Task RevealAsync_ReturnsResolvedEffectiveKey()
    {
        appKeyResolver.Setup(r => r.ResolveAsync(tenantId)).ReturnsAsync("51599bd3-eed3-2826-45a4-a16c2fcc2724");

        TodotixAppKeyRevealDto reveal = await sut.RevealAsync();

        Assert.That(reveal.AppKey, Is.EqualTo("51599bd3-eed3-2826-45a4-a16c2fcc2724"));
    }

    [Test]
    public async Task UpdateAsync_EncryptsAndUpsertsForTenant()
    {
        appKeyCipher.Setup(c => c.Encrypt("51599bd3-eed3-2826-45a4-a16c2fcc2724")).Returns("cipher");
        credentialWriter.Setup(w => w.UpsertAsync(tenantId, "cipher")).Returns(Task.CompletedTask);

        UpdateTodotixAppKeyOutcome outcome = await sut.UpdateAsync(
            new UpdateTodotixAppKeyDto { AppKey = "51599bd3-eed3-2826-45a4-a16c2fcc2724" });

        Assert.That(outcome, Is.TypeOf<UpdateTodotixAppKeyOutcome.Updated>());
        credentialWriter.Verify(w => w.UpsertAsync(tenantId, "cipher"), Times.Once);
    }

    [Test]
    public async Task TestAsync_WhenNoCredentialConfigured_ReportsNotConfigured()
    {
        credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync((TenantPaymentCredential?)null);

        TestTodotixCredentialOutcome outcome = await sut.TestAsync();

        Assert.That(outcome, Is.TypeOf<TestTodotixCredentialOutcome.NotConfigured>());
    }

    [Test]
    public async Task TestAsync_WhenTodotixRegistersDebt_ReportsWorks()
    {
        ArrangeConfiguredCredential();
        todotixClient
            .Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()))
            .ReturnsAsync(new RegisterDebtResponse { Error = 0, QrSimpleUrl = "https://qr" });

        TestTodotixCredentialOutcome outcome = await sut.TestAsync();

        Assert.That(outcome, Is.TypeOf<TestTodotixCredentialOutcome.Works>());
    }

    [Test]
    public async Task TestAsync_WhenTodotixReturnsError_ReportsFailed()
    {
        ArrangeConfiguredCredential();
        todotixClient
            .Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()))
            .ReturnsAsync(new RegisterDebtResponse { Error = 1, QrSimpleUrl = null });

        TestTodotixCredentialOutcome outcome = await sut.TestAsync();

        Assert.That(outcome, Is.TypeOf<TestTodotixCredentialOutcome.Failed>());
    }

    [Test]
    public async Task TestAsync_WhenTodotixSucceedsWithoutQrUrl_ReportsFailed()
    {
        ArrangeConfiguredCredential();
        todotixClient
            .Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()))
            .ReturnsAsync(new RegisterDebtResponse { Error = 0, QrSimpleUrl = null });

        TestTodotixCredentialOutcome outcome = await sut.TestAsync();

        Assert.That(outcome, Is.TypeOf<TestTodotixCredentialOutcome.Failed>());
    }

    [Test]
    public async Task TestAsync_WhenTodotixCallThrows_ReportsFailed()
    {
        ArrangeConfiguredCredential();
        todotixClient
            .Setup(c => c.RegisterDebtAsync(It.IsAny<RegisterDebtRequest>()))
            .ThrowsAsync(new HttpRequestException("unauthorized"));

        TestTodotixCredentialOutcome outcome = await sut.TestAsync();

        Assert.That(outcome, Is.TypeOf<TestTodotixCredentialOutcome.Failed>());
    }

    private void ArrangeConfiguredCredential()
    {
        var credential = new TenantPaymentCredential
        {
            TenantId = tenantId,
            TodotixAppKey = "cipher"
        };
        credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync(credential);
        appKeyCipher.Setup(c => c.Decrypt("cipher")).Returns("51599bd3-eed3-2826-45a4-a16c2fcc2724");
        claimContext.Setup(c => c.TenantTimezone).Returns("America/La_Paz");
        testBuilder
            .Setup(b => b.BuildCredentialTestRequest("51599bd3-eed3-2826-45a4-a16c2fcc2724", "America/La_Paz"))
            .Returns(new RegisterDebtRequest());
    }
}
