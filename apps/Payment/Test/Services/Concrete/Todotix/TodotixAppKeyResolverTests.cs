using Backend.DB.Daos.Abstract.Single.PaymentCredentials;
using Backend.Entities.PaymentCredentials;
using Backend.Security;
using Backend.Services.Concrete.Todotix;

using Moq;

namespace Test.Services.Concrete.Todotix;

[TestFixture]
public class TodotixAppKeyResolverTests
{
    private Mock<ITenantPaymentCredentialReader> credentialReader = null!;
    private Mock<IAppKeyCipher> appKeyCipher = null!;
    private TodotixAppKeyResolver sut = null!;

    [SetUp]
    public void Setup()
    {
        credentialReader = new Mock<ITenantPaymentCredentialReader>(MockBehavior.Strict);
        appKeyCipher = new Mock<IAppKeyCipher>(MockBehavior.Strict);
        sut = new TodotixAppKeyResolver(credentialReader.Object, appKeyCipher.Object);
    }

    [Test]
    public async Task ResolveAsync_WhenCredentialExists_ReturnsDecryptedKey()
    {
        var tenantId = Guid.NewGuid();
        var credential = new TenantPaymentCredential { Id = tenantId, TodotixAppKey = "cipher" };
        credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync(credential);
        appKeyCipher.Setup(c => c.Decrypt("cipher")).Returns("tenant-app-key");

        string? resolved = await sut.ResolveAsync(tenantId);

        Assert.That(resolved, Is.EqualTo("tenant-app-key"));
    }

    [Test]
    public async Task ResolveAsync_WhenNoCredential_ReturnsNull()
    {
        var tenantId = Guid.NewGuid();
        credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync((TenantPaymentCredential?)null);

        string? resolved = await sut.ResolveAsync(tenantId);

        Assert.That(resolved, Is.Null);
        appKeyCipher.Verify(c => c.Decrypt(It.IsAny<string>()), Times.Never);
    }
}
