using Microsoft.IdentityModel.Tokens;

namespace Backend.Services.Abstract;

public interface IJwtTokenSigner
{
    SigningCredentials Credentials { get; }
}
