using System.Security.Cryptography;
using System.Text;

using Backend.Security;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Modules;

public sealed class JwtAuthenticationModule : IServiceModule
{
    public int Order => 50;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["AppSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["AppSettings:Audience"],
                    ValidateLifetime = true,
                    IssuerSigningKey = LoadPublicKey(configuration),
                    ValidateIssuerSigningKey = true,
                    NameClaimType = AuthClaims.UserName,
                    RoleClaimType = AuthClaims.Role,
                };
            });
    }

    private static RsaSecurityKey LoadPublicKey(IConfiguration configuration)
    {
        string publicKeyBase64 = configuration["AppSettings:PublicKey"]
                                 ?? throw new InvalidOperationException("AppSettings:PublicKey not set.");
        string publicKeyPem = Encoding.UTF8.GetString(Convert.FromBase64String(publicKeyBase64));
        RSA rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        return new RsaSecurityKey(rsa);
    }
}
