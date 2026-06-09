using System.ComponentModel.DataAnnotations;

namespace Backend.Options;

public sealed class AttendanceOptions
{
    [Range(1, int.MaxValue)]
    public int PageSize { get; set; } = 10;

    public TimeOnly AllowedWindowStart { get; set; } = new TimeOnly(1, 0);

    public TimeOnly AllowedWindowEnd { get; set; } = new TimeOnly(23, 0);
}
