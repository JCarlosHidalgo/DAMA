using System.Security.Cryptography;
using System.Text;

namespace Backend.Modules;

public sealed class SecretsValidationModule : IServiceModule
{
    public int Order => -100;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        SecretsValidation.RequireRsaPrivateKey(
            configuration["AppSettings:PrivateKey"], "JWT_PRIVATE_KEY_B64");
        SecretsValidation.RequireRsaPublicKey(
            configuration["AppSettings:PublicKey"], "JWT_PUBLIC_KEY_B64");
    }
}

internal static class SecretsValidation
{
    public static void RequireRsaPrivateKey(string? base64Value, string envName)
    {
        RequireNonEmpty(base64Value, envName);
        byte[] pemBytes = DecodeBase64(base64Value!, envName);
        using RSA rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(Encoding.UTF8.GetString(pemBytes));
            rsa.ExportParameters(includePrivateParameters: true);
        }
        catch (Exception inner)
        {
            throw new InvalidOperationException(
                $"Required secret {envName} did not decode to a valid RSA private key.", inner);
        }
    }

    public static void RequireRsaPublicKey(string? base64Value, string envName)
    {
        RequireNonEmpty(base64Value, envName);
        byte[] pemBytes = DecodeBase64(base64Value!, envName);
        using RSA rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(Encoding.UTF8.GetString(pemBytes));
            rsa.ExportParameters(includePrivateParameters: false);
        }
        catch (Exception inner)
        {
            throw new InvalidOperationException(
                $"Required secret {envName} did not decode to a valid RSA public key.", inner);
        }
    }

    public static void RequireMinimumLength(string? value, string envName, int minimumCharacters)
    {
        RequireNonEmpty(value, envName);
        if (value!.Length < minimumCharacters)
        {
            throw new InvalidOperationException(
                $"Required secret {envName} must be at least {minimumCharacters} characters long; got {value.Length}.");
        }
    }

    private static void RequireNonEmpty(string? value, string envName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required secret {envName} is missing. Set it in infrastructure/.env (see infrastructure/SECRETS.md).");
        }
    }

    private static byte[] DecodeBase64(string base64Value, string envName)
    {
        try
        {
            return Convert.FromBase64String(base64Value);
        }
        catch (FormatException inner)
        {
            throw new InvalidOperationException(
                $"Required secret {envName} is not valid base64.", inner);
        }
    }
}
