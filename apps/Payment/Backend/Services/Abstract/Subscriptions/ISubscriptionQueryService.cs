using Backend.Dtos.Subscriptions.Output;
using Backend.Results.QrPayments;

namespace Backend.Services.Abstract.Subscriptions;

public interface ISubscriptionQueryService
{
    Task<GetQrDebtStatusOutcome> GetDebtStatusAsync(Guid paymentId);

    Task<List<SubscriptionPlanDto>> ListPlansAsync();
}
