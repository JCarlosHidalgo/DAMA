using Backend.Dtos.DebtTemplates.Output;

namespace Backend.Results.DebtTemplates;

public abstract record GetDebtTemplateOutcome
{
    private GetDebtTemplateOutcome() { }

    public sealed record Found(DebtTemplateDto Template) : GetDebtTemplateOutcome;

    public sealed record NotFound : GetDebtTemplateOutcome;
}
