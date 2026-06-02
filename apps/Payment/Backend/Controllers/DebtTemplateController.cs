using System.Diagnostics;

using Backend.Dtos.DebtTemplates.Input;
using Backend.Results.DebtTemplates;
using Backend.Security;
using Backend.Services.Abstract.DebtTemplates;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/payment/debt-template")]
[RequiresServiceTier(3)]
public class DebtTemplateController : ControllerBase
{
    private readonly IDebtTemplateService _service;

    public DebtTemplateController(IDebtTemplateService service)
    {
        _service = service;
    }

    [Authorize(Roles = "Client")]
    [HttpPost]
    public async Task<ActionResult> Create(CreateDebtTemplateDto dto)
    {
        CreateDebtTemplateOutcome creationOutcome = await _service.CreateAsync(dto);
        return creationOutcome switch
        {
            CreateDebtTemplateOutcome.Success successOutcome => Ok(successOutcome.Created),
            CreateDebtTemplateOutcome.Replayed replayedOutcome => Ok(replayedOutcome.Existing),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client,Student")]
    [HttpGet]
    public async Task<ActionResult> ListByTenant()
    {
        var templates = await _service.GetByTenantAsync();
        return Ok(templates);
    }

    [Authorize(Roles = "Client")]
    [HttpGet("{templateId:guid}")]
    public async Task<ActionResult> GetById(Guid templateId)
    {
        GetDebtTemplateOutcome getOutcome = await _service.GetByIdAsync(templateId);
        return getOutcome switch
        {
            GetDebtTemplateOutcome.Found foundOutcome => Ok(foundOutcome.Template),
            GetDebtTemplateOutcome.NotFound => NotFound(),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpPut("{templateId:guid}")]
    public async Task<ActionResult> Update(Guid templateId, UpdateDebtTemplateDto dto)
    {
        UpdateDebtTemplateOutcome updateOutcome = await _service.UpdateAsync(templateId, dto);
        return updateOutcome switch
        {
            UpdateDebtTemplateOutcome.Updated => NoContent(),
            UpdateDebtTemplateOutcome.NotFound => NotFound(),
            _ => throw new UnreachableException()
        };
    }

    [Authorize(Roles = "Client")]
    [HttpDelete("{templateId:guid}")]
    public async Task<ActionResult> Delete(Guid templateId)
    {
        DeleteDebtTemplateOutcome deleteOutcome = await _service.DeleteAsync(templateId);
        return deleteOutcome switch
        {
            DeleteDebtTemplateOutcome.Deleted => NoContent(),
            DeleteDebtTemplateOutcome.NotFound => NotFound(),
            _ => throw new UnreachableException()
        };
    }
}
