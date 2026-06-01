using Backend.Dtos.Groups.Output;

namespace Backend.Results.Groups;

public abstract record ListClassGroupsResult
{
    private ListClassGroupsResult() { }

    public sealed record Found(List<GetClassGroupDto> Groups) : ListClassGroupsResult;
}
