using Backend.DB.Daos.Abstract.Single.PaymentCredentials;
using Backend.Entities.PaymentCredentials;
using Backend.Security;
using Backend.Services.Concrete.Todotix;

using Moq;

namespace Test.Services.Concrete.Todotix;

[TestFixture]
public class TodotixAppKeyResolverTests
{
    private Mock<ITenantPaymentCredentialReader> _credentialReader = null!;
    private Mock<IAppKeyCipher> _appKeyCipher = null!;
    private TodotixAppKeyResolver _sut = null!;

    [SetUp]
    public void Setup()
    {
        _credentialReader = new Mock<ITenantPaymentCredentialReader>(MockBehavior.Strict);
        _appKeyCipher = new Mock<IAppKeyCipher>(MockBehavior.Strict);
        _sut = new TodotixAppKeyResolver(_credentialReader.Object, _appKeyCipher.Object);
    }

    [Test]
    public async Task ResolveAsync_WhenCredentialExists_ReturnsDecryptedKey()
    {
        var tenantId = Guid.NewGuid();
        var credential = new TenantPaymentCredential { Id = tenantId, TodotixAppKey = "cipher" };
        _credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync(credential);
        _appKeyCipher.Setup(c => c.Decrypt("cipher")).Returns("tenant-app-key");

        string? resolved = await _sut.ResolveAsync(tenantId);

        Assert.That(resolved, Is.EqualTo("tenant-app-key"));
    }

    [Test]
    public async Task ResolveAsync_WhenNoCredential_ReturnsNull()
    {
        var tenantId = Guid.NewGuid();
        _credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync((TenantPaymentCredential?)null);

        string? resolved = await _sut.ResolveAsync(tenantId);

        Assert.That(resolved, Is.Null);
        _appKeyCipher.Verify(c => c.Decrypt(It.IsAny<string>()), Times.Never);
    }
}
