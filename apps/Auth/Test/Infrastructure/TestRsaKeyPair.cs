using System.Security.Cryptography;
using System.Text;

namespace Test.Infrastructure;

public sealed record TestRsaKeyPair(string PrivateKeyBase64, string PublicKeyBase64)
{
    public static TestRsaKeyPair Generate()
    {
        using var rsa = RSA.Create(2048);

        string privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
        string publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();

        return new TestRsaKeyPair(
            PrivateKeyBase64: Convert.ToBase64String(Encoding.UTF8.GetBytes(privateKeyPem)),
            PublicKeyBase64: Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKeyPem)));
    }
}
