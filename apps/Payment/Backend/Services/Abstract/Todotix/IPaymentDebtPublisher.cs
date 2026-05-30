using Backend.Entities.Todotix;
using Backend.Results.Todotix;

namespace Backend.Services.Abstract.Todotix;

public interface IPaymentDebtPublisher
{
    Task<PublishOutcome> PublishAsync(TodotixOutboxEvent outboxEvent, CancellationToken cancellationToken = default);
}
