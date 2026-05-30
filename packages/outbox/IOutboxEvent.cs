namespace DAMA.Software.MySqlOutbox;

public interface IOutboxEvent
{
    Guid Id { get; }

    DateTime OccurredAt { get; }

    int Attempts { get; set; }
}
