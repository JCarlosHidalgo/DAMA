using System.Diagnostics;

using Backend.Dtos.Users.Input;
using Backend.Dtos.Users.Output;
using Backend.Entities.Users;
using Backend.Pagination;
using Backend.Results.Users;
using Backend.Services.Abstract.Users;
using Backend.Validators.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserRegistrationService _userRegistrationService;
    private readonly IUserDirectoryService _userDirectoryService;

    public AuthController(IAuthenticationService authenticationService,
                          IUserRegistrationService userRegistrationService,
                          IUserDirectoryService userDirectoryService)
    {
        _authenticationService = authenticationService;
        _userRegistrationService = userRegistrationService;
        _userDirectoryService = userDirectoryService;
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpGet("students")]
    public async Task<ActionResult<PagedUsersResponseDto>> GetStudents([FromQuery] PaginationQueryDto query)
    {
        return Ok(await _userDirectoryService.GetStudentsPagedAsync(query.PageIndex));
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpGet("teachers")]
    public async Task<ActionResult<PagedUsersResponseDto>> GetTeachers([FromQuery] PaginationQueryDto query)
    {
        return Ok(await _userDirectoryService.GetTeachersPagedAsync(query.PageIndex));
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpPost("register/teacher")]
    public async Task<ActionResult> RegisterTeacher(RegisterCredentialsDto request)
    {
        RegisterUserOutcome outcome = await _userRegistrationService.RegisterAsync(request, UserRole.Teacher);
        return outcome switch
        {
            RegisterUserOutcome.Created => Ok(),
            RegisterUserOutcome.DuplicateName => BadRequest("Usuario ya existente."),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpPost("register/student")]
    public async Task<ActionResult> RegisterStudent(RegisterCredentialsDto request)
    {
        RegisterUserOutcome outcome = await _userRegistrationService.RegisterAsync(request, UserRole.Student);
        return outcome switch
        {
            RegisterUserOutcome.Created => Ok(),
            RegisterUserOutcome.DuplicateName => BadRequest("Usuario ya existente."),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpPut("users/{id}/username")]
    public async Task<ActionResult> RenameUser(Guid id, UpdateUsernameDto request)
    {
        RenameUserOutcome outcome = await _userDirectoryService.RenameUserAsync(id, request.Username);
        return outcome switch
        {
            RenameUserOutcome.Renamed => NoContent(),
            RenameUserOutcome.DuplicateName => BadRequest("Usuario ya existente."),
            RenameUserOutcome.NotFound => NotFound(),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpDelete("users/{id}")]
    public async Task<ActionResult> DeleteUser(Guid id)
    {
        DeleteUserOutcome outcome = await _userDirectoryService.DeleteUserAsync(id);
        return outcome switch
        {
            DeleteUserOutcome.Deleted => NoContent(),
            DeleteUserOutcome.SelfDeleteForbidden => StatusCode(403, "No puede eliminarse a sí mismo."),
            DeleteUserOutcome.ClientDeleteForbidden => StatusCode(403, "No puede eliminar a otro cliente."),
            DeleteUserOutcome.NotFound => NotFound(),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpGet("students/search")]
    public async Task<ActionResult<UserListItemDto>> SearchStudent([FromQuery] UserSearchQueryDto query)
    {
        UserListItemDto? result = await _userDirectoryService.FindStudentByExactNameAsync(query.Name);
        if (result is null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login(LoginCredentialsDto request)
    {
        TokenResponseDto? result = await _authenticationService.LoginAsync(request);
        if (result is null)
        {
            return Unauthorized(LoginCredentialsDtoValidator.InvalidPayloadMessage);
        }
        return Ok(result);
    }
}
