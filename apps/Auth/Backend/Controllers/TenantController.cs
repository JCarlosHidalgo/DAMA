using System.Diagnostics;

using Backend.Dtos.Tenants.Input;
using Backend.Dtos.Tenants.Output;
using Backend.Results.Tenants;
using Backend.Security;
using Backend.Services.Abstract.Tenants;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/auth/tenants")]
[ApiController]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet]
    public async Task<ActionResult<List<TenantDto>>> GetAll()
    {
        return Ok(await _tenantService.GetAllTenants());
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<TenantDto>> Create(CreateTenantDto request)
    {
        TenantDto tenant = await _tenantService.CreateTenant(request);
        return CreatedAtAction(nameof(GetAll), tenant);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPut("{id}/name")]
    public async Task<ActionResult> UpdateName(Guid id, UpdateTenantNameDto request)
    {
        UpdateTenantNameOutcome outcome = await _tenantService.RenameTenant(id, request.Name);
        return outcome switch
        {
            UpdateTenantNameOutcome.Updated => NoContent(),
            UpdateTenantNameOutcome.NotFound => NotFound(),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Client)]
    [HttpPut("{id}/timezone")]
    public async Task<ActionResult> UpdateTimezone(Guid id, UpdateTenantTimezoneDto request)
    {
        UpdateTenantTimezoneOutcome outcome = await _tenantService.UpdateTenantTimezone(id, request.Timezone);
        return outcome switch
        {
            UpdateTenantTimezoneOutcome.Updated => NoContent(),
            UpdateTenantTimezoneOutcome.Forbidden => StatusCode(403, "No puede modificar otro tenant."),
            UpdateTenantTimezoneOutcome.NotFound => NotFound(),
            _ => throw new UnreachableException()
        };
    }
}
