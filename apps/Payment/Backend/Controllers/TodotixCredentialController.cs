using System.Diagnostics;

using Backend.Dtos.Todotix.Input;
using Backend.Results.Todotix;
using Backend.Services.Abstract.Todotix;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/payment/todotix-credential")]
public class TodotixCredentialController : ControllerBase
{
    private readonly ITodotixCredentialService _service;

    public TodotixCredentialController(ITodotixCredentialService service)
    {
        _service = service;
    }

    [Authorize(Roles = "Client")]
    [HttpGet]
    public async Task<ActionResult> GetStatus()
    {
        var status = await _service.GetStatusAsync();
        return Ok(status);
    }

    [Authorize(Roles = "Student")]
    [HttpGet("availability")]
    public async Task<ActionResult> GetAvailability()
    {
        var availability = await _service.GetAvailabilityAsync();
        return Ok(availability);
    }

    [Authorize(Roles = "Client")]
    [HttpGet("reveal")]
    public async Task<ActionResult> Reveal()
    {
        var reveal = await _service.RevealAsync();
        return Ok(reveal);
    }

    [Authorize(Roles = "Client")]
    [HttpPut]
    public async Task<ActionResult> Update(UpdateTodotixAppKeyDto dto)
    {
        UpdateTodotixAppKeyOutcome updateOutcome = await _service.UpdateAsync(dto);
        return updateOutcome switch
        {
            UpdateTodotixAppKeyOutcome.Updated => NoContent(),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpPost("test")]
    public async Task<ActionResult> Test()
    {
        TestTodotixCredentialOutcome testOutcome = await _service.TestAsync();
        return testOutcome switch
        {
            TestTodotixCredentialOutcome.Works => Ok(),
            TestTodotixCredentialOutcome.NotConfigured => Conflict(),
            TestTodotixCredentialOutcome.Failed => UnprocessableEntity(),
            _ => throw new UnreachableException()
        };
    }
}
