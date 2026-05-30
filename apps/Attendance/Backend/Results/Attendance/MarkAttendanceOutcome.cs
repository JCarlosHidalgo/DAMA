namespace Backend.Results.Attendance;

public abstract record MarkAttendanceOutcome
{
    private MarkAttendanceOutcome() { }

    public sealed record Marked : MarkAttendanceOutcome;

    public sealed record AlreadyMarked : MarkAttendanceOutcome;

    public sealed record NoRemainingClasses : MarkAttendanceOutcome;

    public sealed record InvalidClass : MarkAttendanceOutcome;

    public sealed record ClassFull : MarkAttendanceOutcome;

    public sealed record OutsideAllowedWindow : MarkAttendanceOutcome;

    public sealed record InvalidTenantTimezone : MarkAttendanceOutcome;
}
