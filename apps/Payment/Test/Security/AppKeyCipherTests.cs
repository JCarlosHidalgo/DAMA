using System.Security.Cryptography;

using Backend.Security;

namespace Test.Security;

[TestFixture]
public class AppKeyCipherTests
{
    private static byte[] NewKey() => RandomNumberGenerator.GetBytes(32);

    [Test]
    public void Encrypt_ThenDecrypt_RoundTripsPlaintext()
    {
        AppKeyCipher sut = new AppKeyCipher(NewKey());
        const string plaintext = "51599bd3-eed3-2826-45a4-a16c2fcc2724";

        string encrypted = sut.Encrypt(plaintext);
        string decrypted = sut.Decrypt(encrypted);

        Assert.That(decrypted, Is.EqualTo(plaintext));
    }

    [Test]
    public void Encrypt_SamePlaintextTwice_ProducesDifferentCiphertext()
    {
        AppKeyCipher sut = new AppKeyCipher(NewKey());
        const string plaintext = "51599bd3-eed3-2826-45a4-a16c2fcc2724";

        string first = sut.Encrypt(plaintext);
        string second = sut.Encrypt(plaintext);

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void Decrypt_WithWrongKey_Throws()
    {
        string encrypted = new AppKeyCipher(NewKey()).Encrypt("secret-value");
        AppKeyCipher otherKey = new AppKeyCipher(NewKey());

        Assert.Catch<CryptographicException>(() => otherKey.Decrypt(encrypted));
    }

    [Test]
    public void Decrypt_TamperedPayload_Throws()
    {
        AppKeyCipher sut = new AppKeyCipher(NewKey());
        byte[] payload = Convert.FromBase64String(sut.Encrypt("secret-value"));
        payload[^1] ^= 0xFF;
        string tampered = Convert.ToBase64String(payload);

        Assert.Catch<CryptographicException>(() => sut.Decrypt(tampered));
    }

    [Test]
    public void Constructor_WithNon32ByteKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AppKeyCipher(RandomNumberGenerator.GetBytes(16)));
    }

    [Test]
    public void Decrypt_PayloadShorterThanNonceAndTag_Throws()
    {
        AppKeyCipher sut = new AppKeyCipher(NewKey());
        string tooShort = Convert.ToBase64String(new byte[8]);

        Assert.Catch<CryptographicException>(() => sut.Decrypt(tooShort));
    }
}
