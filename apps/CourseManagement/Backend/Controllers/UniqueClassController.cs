using System.Diagnostics;

using Backend.Application.Mediator;
using Backend.Application.Uniques;
using Backend.Dtos.Uniques.Input;
using Backend.Dtos.Uniques.Output;
using Backend.Results.Uniques;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/course-management/course/unique")]
[ApiController]
public class UniqueClassController : ControllerBase
{
    private const string CourseNotFoundMessage = "Course not found for tenant.";
    private const string UniqueClassNotFoundMessage = "UniqueClass not found for tenant.";

    [Authorize(Roles = "Client")]
    [HttpPost]
    public async Task<ActionResult<GetUniqueClassDto>> CreateUniqueClass(CreateUniqueClassDto createUniqueClassDto,
        [FromServices] ICommandHandler<CreateUniqueClassCommand, CreateUniqueClassResult> handler)
    {
        CreateUniqueClassResult result = await handler.Handle(new CreateUniqueClassCommand(createUniqueClassDto));
        return result switch
        {
            CreateUniqueClassResult.Created created => Ok(created.UniqueClass),
            CreateUniqueClassResult.ReplayedFromIdempotency replayed => Ok(replayed.UniqueClass),
            CreateUniqueClassResult.CourseNotFound => NotFound(CourseNotFoundMessage),
            CreateUniqueClassResult.TeacherConflict conflict => Conflict(BuildTeacherConflictMessage(conflict.TeacherName)),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateUniqueClass(Guid id, UpdateUniqueClassDto updateUniqueClassDto,
        [FromServices] ICommandHandler<UpdateUniqueClassCommand, UpdateUniqueClassResult> handler)
    {
        UpdateUniqueClassResult result = await handler.Handle(new UpdateUniqueClassCommand(id, updateUniqueClassDto));
        return result switch
        {
            UpdateUniqueClassResult.Updated => NoContent(),
            UpdateUniqueClassResult.NotFound => NotFound(UniqueClassNotFoundMessage),
            UpdateUniqueClassResult.TeacherConflict conflict => Conflict(BuildTeacherConflictMessage(conflict.TeacherName)),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUniqueClass(Guid id,
        [FromServices] ICommandHandler<DeleteUniqueClassCommand, DeleteUniqueClassResult> handler)
    {
        DeleteUniqueClassResult result = await handler.Handle(new DeleteUniqueClassCommand(id));
        return result switch
        {
            DeleteUniqueClassResult.Deleted => NoContent(),
            DeleteUniqueClassResult.NotFound => NotFound(UniqueClassNotFoundMessage),
            _ => throw new UnreachableException()
        };
    }

    private static string BuildTeacherConflictMessage(string teacherName) =>
        $"Teacher '{teacherName}' already has a class overlapping the requested time slot.";
}
