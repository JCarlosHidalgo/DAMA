namespace Backend.Security;

public interface IAppKeyCipher
{
    string Encrypt(string plaintext);

    string Decrypt(string encrypted);
}
