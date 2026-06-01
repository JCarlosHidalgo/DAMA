using System.Diagnostics;

using Backend.Application.Courses;
using Backend.Application.Mediator;
using Backend.Application.Schedules;
using Backend.Dtos.Courses.Input;
using Backend.Dtos.Courses.Output;
using Backend.Dtos.Schedules.Input;
using Backend.Dtos.Schedules.Output;
using Backend.Results.Courses;
using Backend.Results.Schedules;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/course-management/course")]
[ApiController]
public class CourseController : ControllerBase
{
    private const string CourseNotFoundMessage = "Course not found for tenant.";

    [Authorize(Roles = "Client,Student")]
    [HttpGet]
    public async Task<ActionResult<List<GetCourseDto>>> GetAllCourses(
        [FromServices] IQueryHandler<ListCoursesQuery, ListCoursesResult> handler)
    {
        ListCoursesResult result = await handler.Handle(new ListCoursesQuery());
        return result switch
        {
            ListCoursesResult.Found found => Ok(found.Courses),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client,Teacher")]
    [HttpGet("{id}")]
    public async Task<ActionResult<GetCourseDto>> GetCourseById(Guid id,
        [FromServices] IQueryHandler<GetCourseByIdQuery, GetCourseByIdResult> handler)
    {
        GetCourseByIdResult result = await handler.Handle(new GetCourseByIdQuery(id));
        return result switch
        {
            GetCourseByIdResult.Found found => Ok(found.Course),
            GetCourseByIdResult.NotFound => NotFound(CourseNotFoundMessage),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpPost]
    public async Task<ActionResult<GetCourseDto>> CreateCourse(CreateCourseDto createCourseDto,
        [FromServices] ICommandHandler<CreateCourseCommand, CreateCourseResult> handler)
    {
        CreateCourseResult result = await handler.Handle(new CreateCourseCommand(createCourseDto));
        return result switch
        {
            CreateCourseResult.Created created => Ok(created.Course),
            CreateCourseResult.ReplayedFromIdempotency replayed => Ok(replayed.Course),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpPut("{id}")]
    public async Task<ActionResult<GetCourseDto>> UpdateCourse(Guid id, UpdateCourseDto updateCourseDto,
        [FromServices] ICommandHandler<UpdateCourseCommand, UpdateCourseResult> handler)
    {
        UpdateCourseResult result = await handler.Handle(new UpdateCourseCommand(id, updateCourseDto));
        return result switch
        {
            UpdateCourseResult.Updated updated => Ok(updated.Course),
            UpdateCourseResult.NotFound => NotFound(CourseNotFoundMessage),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCourse(Guid id,
        [FromServices] ICommandHandler<DeleteCourseCommand, DeleteCourseResult> handler)
    {
        DeleteCourseResult result = await handler.Handle(new DeleteCourseCommand(id));
        return result switch
        {
            DeleteCourseResult.Deleted => NoContent(),
            DeleteCourseResult.NotFound => NotFound(CourseNotFoundMessage),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("teacher/me")]
    public async Task<ActionResult<GetCourseScheduleDto>> GetMyTeacherSchedule(
        [FromQuery] WeekPointerDto requestParameters,
        [FromServices] IQueryHandler<GetTeacherScheduleQuery, GetTeacherScheduleResult> handler)
    {
        DateOnly classDatePointer = ResolveWeekPointer(requestParameters.WeekPaginationIndex);
        GetTeacherScheduleResult result = await handler.Handle(new GetTeacherScheduleQuery(classDatePointer));
        return result switch
        {
            GetTeacherScheduleResult.Found found => Ok(found.Schedule),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpGet("tenant/schedule")]
    public async Task<ActionResult<GetCourseScheduleDto>> GetTenantSchedule(
        [FromQuery] WeekPointerDto requestParameters,
        [FromServices] IQueryHandler<GetTenantScheduleQuery, GetTenantScheduleResult> handler)
    {
        DateOnly classDatePointer = ResolveWeekPointer(requestParameters.WeekPaginationIndex);
        GetTenantScheduleResult result = await handler.Handle(new GetTenantScheduleQuery(classDatePointer));
        return result switch
        {
            GetTenantScheduleResult.Found found => Ok(found.Schedule),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Student")]
    [HttpGet("student/schedule")]
    public async Task<ActionResult<GetCourseScheduleDto>> GetStudentSchedule(
        [FromQuery] WeekPointerDto requestParameters,
        [FromServices] IQueryHandler<GetTenantScheduleQuery, GetTenantScheduleResult> handler)
    {
        DateOnly classDatePointer = ResolveWeekPointer(requestParameters.WeekPaginationIndex);
        GetTenantScheduleResult result = await handler.Handle(new GetTenantScheduleQuery(classDatePointer));
        return result switch
        {
            GetTenantScheduleResult.Found found => Ok(found.Schedule),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpGet("schedule")]
    public async Task<ActionResult<GetCourseScheduleDto>> GetCourseSchedule(
        [FromQuery] CourseScheduleParametersDto requestParameters,
        [FromServices] IQueryHandler<GetCourseScheduleQuery, GetCourseScheduleResult> handler)
    {
        DateOnly classDatePointer = ResolveWeekPointer(requestParameters.WeekPaginationIndex);
        GetCourseScheduleResult result = await handler.Handle(new GetCourseScheduleQuery(requestParameters.CourseId, classDatePointer));
        return result switch
        {
            GetCourseScheduleResult.Found found => Ok(found.Schedule),
            _ => throw new UnreachableException()
        };
    }

    private static DateOnly ResolveWeekPointer(int weekPaginationIndex)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        return today.AddDays(weekPaginationIndex * 7);
    }
}
