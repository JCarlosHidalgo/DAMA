using System.Diagnostics;

using Backend.Application.Mediator;
using Backend.Application.Scheduleds;
using Backend.Dtos.Groups.Input;
using Backend.Dtos.Scheduleds.Input;
using Backend.Dtos.Scheduleds.Output;
using Backend.Results.Scheduleds;
using Backend.Security;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/course-management/course/scheduled")]
[ApiController]
[RequiresServiceTier(1)]
public class ScheduledClassController : ControllerBase
{
    private const string CourseNotFoundMessage = "Course not found for tenant.";
    private const string ScheduledClassNotFoundMessage = "ScheduledClass not found for tenant.";
    private const string GroupNotFoundMessage = "Class group not found for tenant.";
    private const string GroupOverlapMessage = "A class in this group already overlaps the requested time slot.";

    [Authorize(Roles = UserRoles.Client)]
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
            CreateScheduledClassResult.GroupNotFound => NotFound(GroupNotFoundMessage),
            CreateScheduledClassResult.GroupOverlapConflict => Conflict(GroupOverlapMessage),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateScheduledClass(Guid id, UpdateScheduledClassDto updateScheduledClassDto,
        [FromServices] ICommandHandler<UpdateScheduledClassCommand, UpdateScheduledClassResult> handler)
    {
        UpdateScheduledClassResult result = await handler.Handle(new UpdateScheduledClassCommand(id, updateScheduledClassDto));
        return result switch
        {
            UpdateScheduledClassResult.Updated => NoContent(),
            UpdateScheduledClassResult.NotFound => NotFound(ScheduledClassNotFoundMessage),
            UpdateScheduledClassResult.GroupOverlapConflict => Conflict(GroupOverlapMessage),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpPut("{id}/transfer")]
    public async Task<ActionResult> TransferScheduledClass(Guid id, TransferClassDto transferClassDto,
        [FromServices] ICommandHandler<TransferScheduledClassCommand, TransferScheduledClassResult> handler)
    {
        TransferScheduledClassResult result = await handler.Handle(new TransferScheduledClassCommand(id, transferClassDto.TargetGroupId));
        return result switch
        {
            TransferScheduledClassResult.Transferred => NoContent(),
            TransferScheduledClassResult.NotFound => NotFound(ScheduledClassNotFoundMessage),
            TransferScheduledClassResult.GroupNotFound => NotFound(GroupNotFoundMessage),
            TransferScheduledClassResult.GroupOverlapConflict => Conflict(GroupOverlapMessage),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Client)]
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
}
