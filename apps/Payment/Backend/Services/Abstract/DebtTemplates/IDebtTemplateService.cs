using Backend.Dtos.DebtTemplates.Input;
using Backend.Dtos.DebtTemplates.Output;
using Backend.Results.DebtTemplates;

namespace Backend.Services.Abstract.DebtTemplates;

public interface IDebtTemplateService
{
    Task<CreateDebtTemplateOutcome> CreateAsync(CreateDebtTemplateDto dto);

    Task<List<DebtTemplateDto>> GetByTenantAsync();

    Task<GetDebtTemplateOutcome> GetByIdAsync(Guid templateId);

    Task<UpdateDebtTemplateOutcome> UpdateAsync(Guid templateId, UpdateDebtTemplateDto dto);

    Task<DeleteDebtTemplateOutcome> DeleteAsync(Guid templateId);
}
