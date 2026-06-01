using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Entities.Todotix;
using Backend.Options;
using Backend.Security;
using Backend.Services.Concrete.Todotix;

using Microsoft.Extensions.Options;

using Moq;

namespace Test.Services.Concrete.Todotix;

[TestFixture]
public class TodotixAppKeyResolverTests
{
    private Mock<ITenantTodotixCredentialReader> credentialReader = null!;
    private Mock<IAppKeyCipher> appKeyCipher = null!;
    private TodotixOptions todotixOptions = null!;
    private TodotixAppKeyResolver sut = null!;

    [SetUp]
    public void Setup()
    {
        credentialReader = new Mock<ITenantTodotixCredentialReader>(MockBehavior.Strict);
        appKeyCipher = new Mock<IAppKeyCipher>(MockBehavior.Strict);
        todotixOptions = new TodotixOptions { ApplicationKey = "env-app-key", CallbackUrl = "http://cb" };
        sut = new TodotixAppKeyResolver(credentialReader.Object, appKeyCipher.Object, Options.Create(todotixOptions));
    }

    [Test]
    public async Task ResolveAsync_WhenCredentialExists_ReturnsDecryptedKey()
    {
        var tenantId = Guid.NewGuid();
        var credential = new TenantTodotixCredential { TenantId = tenantId, EncryptedAppKey = "cipher" };
        credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync(credential);
        appKeyCipher.Setup(c => c.Decrypt("cipher")).Returns("tenant-app-key");

        string resolved = await sut.ResolveAsync(tenantId);

        Assert.That(resolved, Is.EqualTo("tenant-app-key"));
    }

    [Test]
    public async Task ResolveAsync_WhenNoCredential_FallsBackToEnvKey()
    {
        var tenantId = Guid.NewGuid();
        credentialReader.Setup(r => r.GetByTenantAsync(tenantId)).ReturnsAsync((TenantTodotixCredential?)null);

        string resolved = await sut.ResolveAsync(tenantId);

        Assert.That(resolved, Is.EqualTo("env-app-key"));
        appKeyCipher.Verify(c => c.Decrypt(It.IsAny<string>()), Times.Never);
    }
}
