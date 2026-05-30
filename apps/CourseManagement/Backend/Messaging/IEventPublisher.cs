using Backend.Entities;

namespace Backend.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken);
}
