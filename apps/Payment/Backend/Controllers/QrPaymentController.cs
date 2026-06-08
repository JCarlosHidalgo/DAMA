using System.Diagnostics;

using Backend.Application.Commands;
using Backend.Application.Mediator;
using Backend.Common;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Dtos.QrPayments.Input;
using Backend.Dtos.QrPayments.Output;
using Backend.Logging;
using Backend.Results.QrPayments;
using Backend.Security;
using Backend.Services.Abstract;
using Backend.Services.Abstract.QrPayments;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/payment/qr")]
public class QrPaymentController : ControllerBase
{
    private readonly ICommandHandler<CreateClassQrDebtCommand, CreateQrDebtOutcome> _createDebtHandler;
    private readonly IQrPaymentQueryService _queryService;
    private readonly IPaymentCallbackInboxDao _callbackInbox;
    private readonly ICallbackSignature _callbackSignature;
    private readonly ILogger<QrPaymentController> _logger;

    public QrPaymentController(ICommandHandler<CreateClassQrDebtCommand, CreateQrDebtOutcome> createDebtHandler,
                               IQrPaymentQueryService queryService,
                               IPaymentCallbackInboxDao callbackInbox,
                               ICallbackSignature callbackSignature,
                               ILogger<QrPaymentController> logger)
    {
        _createDebtHandler = createDebtHandler;
        _queryService = queryService;
        _callbackInbox = callbackInbox;
        _callbackSignature = callbackSignature;
        _logger = logger;
    }

    [Authorize(Roles = UserRoles.Student)]
    [RequiresServiceTier(3)]
    [HttpPost("{templateId:guid}")]
    public async Task<ActionResult> CreateDebt(Guid templateId, CreateQrDebtDto dto)
    {
        string effectiveEmail = string.IsNullOrWhiteSpace(dto.Email) ? "example@gmail.com" : dto.Email;
        CreateQrDebtOutcome creationOutcome =
            await _createDebtHandler.Handle(new CreateClassQrDebtCommand(templateId, effectiveEmail, dto));
        return creationOutcome switch
        {
            CreateQrDebtOutcome.Success successOutcome => Accepted(successOutcome.Created),
            CreateQrDebtOutcome.TemplateNotFound => NotFound("Template no existe para este tenant."),
            CreateQrDebtOutcome.PaymentNotConfigured => Conflict("El tenant no tiene una credencial de cobro configurada."),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Student)]
    [RequiresServiceTier(3)]
    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult> GetDebtStatus(Guid id)
    {
        GetQrDebtStatusOutcome statusOutcome = await _queryService.GetDebtStatusAsync(id);
        return statusOutcome switch
        {
            GetQrDebtStatusOutcome.Found foundOutcome => Ok(foundOutcome.Status),
            GetQrDebtStatusOutcome.NotFound => NotFound(),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = UserRoles.Student)]
    [RequiresServiceTier(3)]
    [HttpGet("pending")]
    public async Task<ActionResult> ListPending([FromQuery] PaginationParamsDto pagination)
    {
        var paginatedPayments = await _queryService.ListPendingAsync(pagination.Index);
        return Ok(paginatedPayments);
    }

    [Authorize(Roles = UserRoles.Student)]
    [RequiresServiceTier(3)]
    [HttpGet("success")]
    public async Task<ActionResult> ListSuccess([FromQuery] PaginationParamsDto pagination)
    {
        var paginatedPayments = await _queryService.ListSuccessAsync(pagination.Index);
        return Ok(paginatedPayments);
    }

    [Authorize(Roles = UserRoles.Student)]
    [RequiresServiceTier(3)]
    [HttpGet("failed")]
    public async Task<ActionResult> ListFailed([FromQuery] PaginationParamsDto pagination)
    {
        var paginatedPayments = await _queryService.ListFailedAsync(pagination.Index);
        return Ok(paginatedPayments);
    }

    [Authorize(Roles = UserRoles.Student)]
    [RequiresServiceTier(3)]
    [HttpGet("analytics/status")]
    public async Task<ActionResult> GetStatusBreakdown()
    {
        StudentQrBreakdownDto breakdown = await _queryService.GetStatusBreakdownAsync();
        return Ok(breakdown);
    }

    [Authorize(Roles = UserRoles.Student)]
    [RequiresServiceTier(3)]
    [HttpGet("analytics/spend")]
    public async Task<ActionResult> GetSpend([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        DateTime rangeEnd = to ?? DateTime.UtcNow;
        DateTime rangeStart = from ?? rangeEnd.AddMonths(-12);
        List<StudentSpendPointDto> spendPoints = await _queryService.GetSpendByMonthAsync(rangeStart, rangeEnd);
        return Ok(spendPoints);
    }

    [HttpGet("callback")]
    public async Task<ActionResult> Callback([FromQuery(Name = "transaction_id")] Guid transactionId,
                                              [FromQuery] int error,
                                              [FromQuery(Name = "cancel_order")] int cancelOrder,
                                              [FromQuery(Name = "sig")] string? signature)
    {
        if (!_callbackSignature.Verify(transactionId.ToString("D"), signature ?? string.Empty))
        {
            LogEvents.TodotixCallbackInvalidSignature(_logger, transactionId);
            return Ok();
        }

        await _callbackInbox.TryEnqueueAsync(transactionId, error, cancelOrder);
        return Ok();
    }

}
