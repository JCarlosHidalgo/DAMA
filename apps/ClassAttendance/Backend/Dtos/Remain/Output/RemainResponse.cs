namespace Backend.Dtos.Remain.Output;

public sealed class RemainResponse
{
    public Guid StudentId { get; set; }

    public int NumberOfClasses { get; set; }

    public string? StudentName { get; set; }
}
