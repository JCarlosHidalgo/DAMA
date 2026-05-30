namespace Backend.Results.Remain;

public abstract record IncrementTenantRemainOutcome
{
    private IncrementTenantRemainOutcome() { }

    public sealed record Applied(int Affected) : IncrementTenantRemainOutcome;

    public sealed record AlreadyApplied : IncrementTenantRemainOutcome;
}
