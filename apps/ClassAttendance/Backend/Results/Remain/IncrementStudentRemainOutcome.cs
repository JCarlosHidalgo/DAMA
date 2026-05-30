namespace Backend.Results.Remain;

public abstract record IncrementStudentRemainOutcome
{
    private IncrementStudentRemainOutcome() { }

    public sealed record Applied : IncrementStudentRemainOutcome;

    public sealed record AlreadyApplied : IncrementStudentRemainOutcome;
}
