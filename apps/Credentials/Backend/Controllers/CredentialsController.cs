using Backend.Dtos.Output;
using Backend.Security;
using Backend.Services.Abstract;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/credentials")]
[ApiController]
public class CredentialsController : ControllerBase
{
    private readonly ICredentialsService _credentialsService;

    public CredentialsController(ICredentialsService credentialsService)
    {
        _credentialsService = credentialsService;
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpGet("client-credentials")]
    public async Task<ActionResult<UserClaimsDto>> GetClientCredentials()
    {
        UserClaimsDto claims = await _credentialsService.GetCredentials();
        return Ok(claims);
    }

    [Authorize(Roles = UserRoles.Teacher)]
    [HttpGet("teacher-credentials")]
    public async Task<ActionResult<UserClaimsDto>> GetTeacherCredentials()
    {
        UserClaimsDto claims = await _credentialsService.GetCredentials();
        return Ok(claims);
    }

    [Authorize(Roles = UserRoles.Student)]
    [HttpGet("student-credentials")]
    public async Task<ActionResult<UserClaimsDto>> GetStudentCredentials()
    {
        UserClaimsDto claims = await _credentialsService.GetCredentials();
        return Ok(claims);
    }
}
