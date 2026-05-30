namespace Backend.Dtos.Remain.Input;

public interface IRemainIncrement
{
    Guid RequestId { get; }

    int Quantity { get; }
}
