using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Dtos.Todotix.Input;
using Backend.Dtos.Todotix.Output;
using Backend.Entities.Todotix;
using Backend.Results.Todotix;
using Backend.Security;
using Backend.Services.Abstract.Todotix;
using Backend.Services.Concrete.Todotix;

using Moq;

namespace Test.Services.Concrete.Todotix;

[TestFixture]
public class TodotixCredentialServiceTests
{
    private Mock<ITenantTodotixCredentialReader> credentialReader = null!;
    private Mock<ITenantTodotixCredentialWriter> credentialWriter = null!;
    private Mock<IAppKeyCipher> appKeyCipher = null!;
    private Mock<ITodotixAppKeyResolver> appKeyResolver = null!;
    private Mock<IClaimContext> claimContext = null!;
    private TodotixCredentialService sut = null!;
    private Guid tenantId;

    [SetUp]
    public void Setup()
    {
        credentialReader = new Mock<ITenantTodotixCredentialReader>(MockBehavior.Strict);
        credentialWriter = new Mock<ITenantTodotixCredentialWriter>(MockBehavior.Strict);
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
        var credential = new TenantTodotixCredential
        {
            TenantId = tenantId,
            EncryptedAppKey = "cipher",
            UpdatedAt = new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc)
        };
        credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync(credential);
        appKeyResolver.Setup(r => r.ResolveAsync(tenantId)).ReturnsAsync("51599bd3-eed3-2826-45a4-a16c2fcc2724");

        TodotixAppKeyStatusDto status = await sut.GetStatusAsync();

        Assert.Multiple(() =>
        {
            Assert.That(status.HasCustomKey, Is.True);
            Assert.That(status.MaskedAppKey, Does.EndWith("2724"));
            Assert.That(status.MaskedAppKey, Does.StartWith("•"));
            Assert.That(status.UpdatedAt, Is.EqualTo(credential.UpdatedAt));
        });
    }

    [Test]
    public async Task GetStatusAsync_WhenNoCustomKey_ReportsUsingEnvKey()
    {
        credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync((TenantTodotixCredential?)null);
        appKeyResolver.Setup(r => r.ResolveAsync(tenantId)).ReturnsAsync("env-app-key");

        TodotixAppKeyStatusDto status = await sut.GetStatusAsync();

        Assert.Multiple(() =>
        {
            Assert.That(status.HasCustomKey, Is.False);
            Assert.That(status.UpdatedAt, Is.Null);
        });
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
