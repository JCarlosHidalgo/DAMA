using Backend.Events;
using Backend.Results.Events;

namespace Backend.Services.Abstract.Events;

public interface IPaymentCapturedHandler
{
    Task<PaymentCapturedOutcome> HandleAsync(PaymentCapturedEvent paymentCapturedEvent, CancellationToken cancellationToken);
}
