namespace Backend.Dtos.Schedules.Input;

public class CourseScheduleParametersDto
{
    public required Guid CourseId { get; set; }

    public int WeekPaginationIndex { get; set; } = 0;
}
