namespace Backend.Dtos.Remain.Input;

public class IncrementTenantRemainDto : IRemainIncrement
{
    public required Guid RequestId { get; set; }

    public required int Quantity { get; set; }
}
