namespace Backend.Results.Events;

public abstract record CourseDeletedOutcome
{
    private CourseDeletedOutcome() { }

    public sealed record AttendancesDeleted : CourseDeletedOutcome;

    public sealed record AlreadyProcessed : CourseDeletedOutcome;

    public sealed record Failed : CourseDeletedOutcome;
}
