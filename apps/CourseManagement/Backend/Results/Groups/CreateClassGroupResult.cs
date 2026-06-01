using Backend.Dtos.Groups.Output;

namespace Backend.Results.Groups;

public abstract record CreateClassGroupResult
{
    private CreateClassGroupResult() { }

    public sealed record Created(GetClassGroupDto Group) : CreateClassGroupResult;
}
