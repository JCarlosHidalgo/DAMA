using Backend.Dtos.Output;

namespace Backend.Services.Abstract;

public interface ICredentialsService
{
    Task<UserClaimsDto> GetCredentials();
}
