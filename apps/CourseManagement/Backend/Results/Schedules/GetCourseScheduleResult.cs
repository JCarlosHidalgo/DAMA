using Backend.Dtos.Schedules.Output;

namespace Backend.Results.Schedules;

public abstract record GetCourseScheduleResult
{
    private GetCourseScheduleResult() { }

    public sealed record Found(GetCourseScheduleDto Schedule) : GetCourseScheduleResult;
}
