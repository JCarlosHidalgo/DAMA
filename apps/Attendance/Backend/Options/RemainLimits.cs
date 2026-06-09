using System.ComponentModel.DataAnnotations;

namespace Backend.Options;

public sealed class RemainLimits
{
    [Range(1, int.MaxValue)]
    public int MinIncrement { get; set; } = 1;

    [Range(1, int.MaxValue)]
    public int MaxIncrement { get; set; } = 49;

    [Range(1, int.MaxValue)]
    public int MaxStudentNameLength { get; set; } = 80;
}
