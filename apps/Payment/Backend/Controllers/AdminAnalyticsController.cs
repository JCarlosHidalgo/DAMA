using Backend.Dtos.Admin.Output;
using Backend.Security;
using Backend.Services.Abstract.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/payment/admin/analytics")]
[Authorize(Roles = UserRoles.Admin)]
public class AdminAnalyticsController : ControllerBase
{
    private readonly IAdminAnalyticsService _analyticsService;

    public AdminAnalyticsController(IAdminAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("revenue/total")]
    public async Task<ActionResult> GetRevenueTotal()
    {
        SubscriptionRevenueTotalDto total = await _analyticsService.GetRevenueTotalAsync();
        return Ok(total);
    }

    [HttpGet("revenue/timeline")]
    public async Task<ActionResult> GetRevenueTimeline([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        DateTime rangeEnd = to ?? DateTime.UtcNow;
        DateTime rangeStart = from ?? rangeEnd.AddMonths(-12);
        List<SubscriptionRevenuePointDto> timeline = await _analyticsService.GetRevenueTimelineAsync(rangeStart, rangeEnd);
        return Ok(timeline);
    }

    [HttpGet("revenue/by-tier")]
    public async Task<ActionResult> GetRevenueByTier()
    {
        List<SubscriptionRevenueByTierDto> byTier = await _analyticsService.GetRevenueByTierAsync();
        return Ok(byTier);
    }
}
