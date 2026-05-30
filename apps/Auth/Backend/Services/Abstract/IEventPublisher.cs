using Backend.Entities;

namespace Backend.Services.Abstract;

public interface IEventPublisher
{
    Task PublishAsync(OutboxEvent evt, CancellationToken ct);
}
