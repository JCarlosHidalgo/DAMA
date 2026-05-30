using Backend.Dtos.Schedules.Output;

namespace Backend.Results.Schedules;

public abstract record GetTeacherScheduleResult
{
    private GetTeacherScheduleResult() { }

    public sealed record Found(GetCourseScheduleDto Schedule) : GetTeacherScheduleResult;
}
