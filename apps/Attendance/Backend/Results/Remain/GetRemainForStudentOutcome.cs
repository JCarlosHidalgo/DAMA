using Backend.Dtos.Remain.Output;

namespace Backend.Results.Remain;

public abstract record GetRemainForStudentOutcome
{
    private GetRemainForStudentOutcome() { }

    public sealed record Found(RemainResponse Remain) : GetRemainForStudentOutcome;

    public sealed record Forbidden : GetRemainForStudentOutcome;
}
