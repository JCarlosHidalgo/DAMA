namespace Backend.Results.Events;

public abstract record ClassDeletedOutcome
{
    private ClassDeletedOutcome() { }

    public sealed record AttendancesDeleted : ClassDeletedOutcome;

    public sealed record AlreadyProcessed : ClassDeletedOutcome;

    public sealed record Failed : ClassDeletedOutcome;
}
