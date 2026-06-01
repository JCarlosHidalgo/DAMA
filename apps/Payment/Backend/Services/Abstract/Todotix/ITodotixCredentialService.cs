using Backend.Dtos.Todotix.Input;
using Backend.Dtos.Todotix.Output;
using Backend.Results.Todotix;

namespace Backend.Services.Abstract.Todotix;

public interface ITodotixCredentialService
{
    Task<TodotixAppKeyStatusDto> GetStatusAsync();

    Task<PaymentAvailabilityDto> GetAvailabilityAsync();

    Task<TodotixAppKeyRevealDto> RevealAsync();

    Task<UpdateTodotixAppKeyOutcome> UpdateAsync(UpdateTodotixAppKeyDto dto);

    Task<TestTodotixCredentialOutcome> TestAsync();
}
