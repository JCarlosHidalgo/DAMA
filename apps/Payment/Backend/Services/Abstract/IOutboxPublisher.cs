namespace Backend.Services.Abstract;

public interface IOutboxPublisher<TOutboxEvent>
{
    Task PublishAsync(TOutboxEvent outboxEvent, CancellationToken cancellationToken);
}
