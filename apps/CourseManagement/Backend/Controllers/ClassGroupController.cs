using System.Diagnostics;

using Backend.Application.Groups;
using Backend.Application.Mediator;
using Backend.Dtos.Groups.Input;
using Backend.Dtos.Groups.Output;
using Backend.Results.Groups;
using Backend.Security;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/course-management/course/group")]
[ApiController]
[RequiresServiceTier(1)]
public class ClassGroupController : ControllerBase
{
    private const string GroupNotFoundMessage = "Class group not found for tenant.";
    private const string GroupNotEmptyMessage = "Class group still has classes; move or delete them before deleting the group.";

    [Authorize(Roles = "Client,Student")]
    [HttpGet]
    public async Task<ActionResult<List<GetClassGroupDto>>> GetGroups(
        [FromServices] IQueryHandler<ListClassGroupsQuery, ListClassGroupsResult> handler)
    {
        ListClassGroupsResult result = await handler.Handle(new ListClassGroupsQuery());
        return result switch
        {
            ListClassGroupsResult.Found found => Ok(found.Groups),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet("teacher/me")]
    public async Task<ActionResult<List<GetClassGroupDto>>> GetMyTeacherGroups(
        [FromServices] IQueryHandler<ListTeacherClassGroupsQuery, ListClassGroupsResult> handler)
    {
        ListClassGroupsResult result = await handler.Handle(new ListTeacherClassGroupsQuery());
        return result switch
        {
            ListClassGroupsResult.Found found => Ok(found.Groups),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpPost]
    public async Task<ActionResult<GetClassGroupDto>> CreateGroup(CreateClassGroupDto createClassGroupDto,
        [FromServices] ICommandHandler<CreateClassGroupCommand, CreateClassGroupResult> handler)
    {
        CreateClassGroupResult result = await handler.Handle(new CreateClassGroupCommand(createClassGroupDto));
        return result switch
        {
            CreateClassGroupResult.Created created => Ok(created.Group),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateGroup(Guid id, UpdateClassGroupDto updateClassGroupDto,
        [FromServices] ICommandHandler<UpdateClassGroupCommand, UpdateClassGroupResult> handler)
    {
        UpdateClassGroupResult result = await handler.Handle(new UpdateClassGroupCommand(id, updateClassGroupDto));
        return result switch
        {
            UpdateClassGroupResult.Updated => NoContent(),
            UpdateClassGroupResult.NotFound => NotFound(GroupNotFoundMessage),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteGroup(Guid id,
        [FromServices] ICommandHandler<DeleteClassGroupCommand, DeleteClassGroupResult> handler)
    {
        DeleteClassGroupResult result = await handler.Handle(new DeleteClassGroupCommand(id));
        return result switch
        {
            DeleteClassGroupResult.Deleted => NoContent(),
            DeleteClassGroupResult.NotFound => NotFound(GroupNotFoundMessage),
            DeleteClassGroupResult.GroupNotEmpty => Conflict(GroupNotEmptyMessage),
            _ => throw new UnreachableException()
        };
    }
}
