using System.Security.Cryptography;
using System.Text;

using Backend.Options;
using Backend.Services.Abstract;

using Microsoft.Extensions.Options;

namespace Backend.Services.Concrete;

public sealed class CallbackSignature(IOptions<PaymentCallbackOptions> callbackOptions) : ICallbackSignature
{
    public string Sign(string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        byte[] hash = HMACSHA256.HashData(LoadSecret(), Encoding.UTF8.GetBytes(payload));
        return ToBase64Url(hash);
    }

    public bool Verify(string payload, string providedSignature)
    {
        if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(providedSignature))
        {
            return false;
        }

        byte[] expected = HMACSHA256.HashData(LoadSecret(), Encoding.UTF8.GetBytes(payload));
        byte[] provided;
        try
        {
            provided = FromBase64Url(providedSignature);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }

    private byte[] LoadSecret()
    {
        return Encoding.UTF8.GetBytes(callbackOptions.Value.Secret);
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static byte[] FromBase64Url(string value)
    {
        string padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
            case 1:
                throw new FormatException("Invalid base64url length.");
        }
        return Convert.FromBase64String(padded);
    }
}
