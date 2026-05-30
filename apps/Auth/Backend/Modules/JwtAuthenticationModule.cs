using System.Security.Cryptography;
using System.Text;

using Backend.Options;
using Backend.Security;
using Backend.Services.Abstract;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Modules;

public sealed class JwtAuthenticationModule : IServiceModule
{
    public int Order => 50;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IJwtTokenSigner, JwtTokenSigner>();
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
            {
                JwtOptions jwtOptions = jwtOptionsAccessor.Value;
                bearerOptions.MapInboundClaims = false;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = LoadPublicKey(jwtOptions.PublicKey),
                    ValidateIssuerSigningKey = true,
                    NameClaimType = AuthClaims.UserName,
                    RoleClaimType = AuthClaims.Role,
                };
            });
    }

    private static RsaSecurityKey LoadPublicKey(string publicKeyBase64)
    {
        string publicKeyPem = Encoding.UTF8.GetString(Convert.FromBase64String(publicKeyBase64));
        RSA rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        return new RsaSecurityKey(rsa);
    }
}
