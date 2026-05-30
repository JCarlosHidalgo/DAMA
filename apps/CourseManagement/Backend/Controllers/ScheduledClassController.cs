using System.Diagnostics;

using Backend.Application.Mediator;
using Backend.Application.Scheduleds;
using Backend.Dtos.Scheduleds.Input;
using Backend.Dtos.Scheduleds.Output;
using Backend.Results.Scheduleds;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/course-management/course/scheduled")]
[ApiController]
public class ScheduledClassController : ControllerBase
{
    private const string CourseNotFoundMessage = "Course not found for tenant.";
    private const string ScheduledClassNotFoundMessage = "ScheduledClass not found for tenant.";

    [Authorize(Roles = "Client")]
    [HttpPost]
    public async Task<ActionResult<GetScheduledClassDto>> CreateScheduledClass(CreateScheduledClassDto createScheduledClassDto,
        [FromServices] ICommandHandler<CreateScheduledClassCommand, CreateScheduledClassResult> handler)
    {
        CreateScheduledClassResult result = await handler.Handle(new CreateScheduledClassCommand(createScheduledClassDto));
        return result switch
        {
            CreateScheduledClassResult.Created created => Ok(created.ScheduledClass),
            CreateScheduledClassResult.ReplayedFromIdempotency replayed => Ok(replayed.ScheduledClass),
            CreateScheduledClassResult.CourseNotFound => NotFound(CourseNotFoundMessage),
            CreateScheduledClassResult.TeacherConflict conflict => Conflict(BuildTeacherConflictMessage(conflict.TeacherName)),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateScheduledClass(Guid id, UpdateScheduledClassDto updateScheduledClassDto,
        [FromServices] ICommandHandler<UpdateScheduledClassCommand, UpdateScheduledClassResult> handler)
    {
        UpdateScheduledClassResult result = await handler.Handle(new UpdateScheduledClassCommand(id, updateScheduledClassDto));
        return result switch
        {
            UpdateScheduledClassResult.Updated => NoContent(),
            UpdateScheduledClassResult.NotFound => NotFound(ScheduledClassNotFoundMessage),
            UpdateScheduledClassResult.TeacherConflict conflict => Conflict(BuildTeacherConflictMessage(conflict.TeacherName)),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteScheduledClass(Guid id,
        [FromServices] ICommandHandler<DeleteScheduledClassCommand, DeleteScheduledClassResult> handler)
    {
        DeleteScheduledClassResult result = await handler.Handle(new DeleteScheduledClassCommand(id));
        return result switch
        {
            DeleteScheduledClassResult.Deleted => NoContent(),
            DeleteScheduledClassResult.NotFound => NotFound(ScheduledClassNotFoundMessage),
            _ => throw new UnreachableException()
        };
    }

    private static string BuildTeacherConflictMessage(string teacherName) =>
        $"Teacher '{teacherName}' already has a class overlapping the requested time slot.";
}
