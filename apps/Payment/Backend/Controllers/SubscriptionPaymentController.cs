using System.Diagnostics;

using Backend.Application.Commands;
using Backend.Application.Mediator;
using Backend.Application.Results;
using Backend.Dtos.Subscriptions.Input;
using Backend.Results.QrPayments;
using Backend.Services.Abstract.Subscriptions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/payment/subscription")]
public class SubscriptionPaymentController : ControllerBase
{
    private readonly ICommandHandler<CreateSubscriptionQrDebtCommand, CreateSubscriptionDebtOutcome> _createDebtHandler;
    private readonly ISubscriptionQueryService _queryService;
    private readonly ISubscriptionPlanService _planService;

    public SubscriptionPaymentController(
        ICommandHandler<CreateSubscriptionQrDebtCommand, CreateSubscriptionDebtOutcome> createDebtHandler,
        ISubscriptionQueryService queryService,
        ISubscriptionPlanService planService)
    {
        _createDebtHandler = createDebtHandler;
        _queryService = queryService;
        _planService = planService;
    }

    [Authorize(Roles = "Client")]
    [HttpPost("qr")]
    public async Task<ActionResult> CreateDebt(CreateSubscriptionDebtDto dto)
    {
        string effectiveEmail = string.IsNullOrWhiteSpace(dto.Email) ? "example@gmail.com" : dto.Email;
        CreateSubscriptionDebtOutcome outcome =
            await _createDebtHandler.Handle(new CreateSubscriptionQrDebtCommand(dto.Level, effectiveEmail));
        return outcome switch
        {
            CreateSubscriptionDebtOutcome.Success success => Accepted(success.Created),
            CreateSubscriptionDebtOutcome.PlanNotFound => NotFound("No existe un plan para ese nivel."),
            CreateSubscriptionDebtOutcome.PaymentNotConfigured => Conflict("El cobro de suscripciones no está configurado."),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpGet("qr/{id:guid}/status")]
    public async Task<ActionResult> GetDebtStatus(Guid id)
    {
        GetQrDebtStatusOutcome outcome = await _queryService.GetDebtStatusAsync(id);
        return outcome switch
        {
            GetQrDebtStatusOutcome.Found found => Ok(found.Status),
            GetQrDebtStatusOutcome.NotFound => NotFound(),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client,Admin")]
    [HttpGet("plans")]
    public async Task<ActionResult> ListPlans()
    {
        return Ok(await _queryService.ListPlansAsync());
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("plans/{level:int}")]
    public async Task<ActionResult> UpdatePlan(int level, UpdateSubscriptionPlanDto dto)
    {
        await _planService.UpdateAsync(level, dto);
        return NoContent();
    }
}
