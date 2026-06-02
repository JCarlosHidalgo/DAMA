using Backend.Security;
using Backend.Services.Abstract.Summary;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/payment/summary")]
[RequiresServiceTier(3)]
public class SummaryController : ControllerBase
{
    private readonly ISummaryService _service;

    public SummaryController(ISummaryService service)
    {
        _service = service;
    }

    [Authorize(Roles = "Client")]
    [HttpGet]
    public async Task<ActionResult> GetSummary()
    {
        var summary = await _service.GetByTenantAsync();
        return Ok(summary);
    }
}
