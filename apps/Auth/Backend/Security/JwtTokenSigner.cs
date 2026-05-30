using System.Security.Cryptography;
using System.Text;

using Backend.Options;
using Backend.Services.Abstract;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Security;

public sealed class JwtTokenSigner : IJwtTokenSigner, IDisposable
{
    private readonly RSA _rsa;

    public SigningCredentials Credentials { get; }

    public JwtTokenSigner(IOptions<JwtOptions> options)
    {
        string privateKeyB64 = options.Value.PrivateKey;
        string privateKeyPem = Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyB64));

        _rsa = RSA.Create();
        _rsa.ImportFromPem(privateKeyPem);

        RsaSecurityKey securityKey = new RsaSecurityKey(_rsa);
        Credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
    }

    public void Dispose()
    {
        _rsa.Dispose();
    }
}
