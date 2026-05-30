using Backend.Dtos.Schedules.Output;

namespace Backend.Results.Schedules;

public abstract record GetTenantScheduleResult
{
    private GetTenantScheduleResult() { }

    public sealed record Found(GetCourseScheduleDto Schedule) : GetTenantScheduleResult;
}
