namespace Backend.Options;

public sealed class RemainLimits
{
    public int MinIncrement { get; set; } = 1;

    public int MaxIncrement { get; set; } = 49;

    public int MaxStudentNameLength { get; set; } = 80;
}
