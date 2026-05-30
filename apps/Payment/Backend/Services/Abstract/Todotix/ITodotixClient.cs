using Backend.Dtos.External.Todotix;

namespace Backend.Services.Abstract.Todotix;

public interface ITodotixClient
{
    Task<RegisterDebtResponse> RegisterDebtAsync(RegisterDebtRequest request);

    Task<bool> DebtExistsAsync(Guid debtIdentifier);

    Task<TodotixDebtState> ConsultDebtAsync(Guid debtIdentifier);
}
