using System.Diagnostics;

using Backend.Dtos.Tenants.Input;
using Backend.Entities.Users;
using Backend.Results.Tenants;
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
