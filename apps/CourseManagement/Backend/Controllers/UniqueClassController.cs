using System.Diagnostics;

using Backend.Application.Mediator;
using Backend.Application.Uniques;
using Backend.Dtos.Groups.Input;
using Backend.Dtos.Uniques.Input;
using Backend.Dtos.Uniques.Output;
using Backend.Results.Uniques;
using Backend.Security;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/course-management/course/unique")]
[ApiController]
[RequiresServiceTier(1)]
public class UniqueClassController : ControllerBase
{
    private const string CourseNotFoundMessage = "Course not found for tenant.";
    private const string UniqueClassNotFoundMessage = "UniqueClass not found for tenant.";
    private const string GroupNotFoundMessage = "Class group not found for tenant.";
    private const string GroupOverlapMessage = "A class in this group already overlaps the requested time slot.";

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
            CreateUniqueClassResult.GroupNotFound => NotFound(GroupNotFoundMessage),
            CreateUniqueClassResult.GroupOverlapConflict => Conflict(GroupOverlapMessage),
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
            UpdateUniqueClassResult.GroupOverlapConflict => Conflict(GroupOverlapMessage),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpPut("{id}/transfer")]
    public async Task<ActionResult> TransferUniqueClass(Guid id, TransferClassDto transferClassDto,
        [FromServices] ICommandHandler<TransferUniqueClassCommand, TransferUniqueClassResult> handler)
    {
        TransferUniqueClassResult result = await handler.Handle(new TransferUniqueClassCommand(id, transferClassDto.TargetGroupId));
        return result switch
        {
            TransferUniqueClassResult.Transferred => NoContent(),
            TransferUniqueClassResult.NotFound => NotFound(UniqueClassNotFoundMessage),
            TransferUniqueClassResult.GroupNotFound => NotFound(GroupNotFoundMessage),
            TransferUniqueClassResult.GroupOverlapConflict => Conflict(GroupOverlapMessage),
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
}
