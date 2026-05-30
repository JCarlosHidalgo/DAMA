namespace Backend.Dtos.Remain.Input;

public class IncrementStudentRemainDto : IRemainIncrement
{
    public required Guid RequestId { get; set; }

    public required int Quantity { get; set; }

    public string? StudentName { get; set; }
}
