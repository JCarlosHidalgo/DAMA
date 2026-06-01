using Backend.Dtos.External.Todotix;

namespace Backend.Services.Abstract.Todotix;

public interface ITodotixClient
{
    Task<RegisterDebtResponse> RegisterDebtAsync(RegisterDebtRequest request);

    Task<bool> DebtExistsAsync(Guid debtIdentifier, string appKey);

    Task<TodotixDebtState> ConsultDebtAsync(Guid debtIdentifier, string appKey);
}
