using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.PaymentCredentials;
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
        tenantId = Guid.NewGuid();
        claimContext.Setup(c => c.TenantId).Returns(tenantId);

        sut = new TodotixCredentialService(
            credentialReader.Object,
            credentialWriter.Object,
            appKeyCipher.Object,
            appKeyResolver.Object,
            claimContext.Object,
            new TodotixCredentialViewBuilder());
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
}
