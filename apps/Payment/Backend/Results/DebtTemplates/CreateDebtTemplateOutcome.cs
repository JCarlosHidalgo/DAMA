using Backend.Dtos.DebtTemplates.Output;

namespace Backend.Results.DebtTemplates;

public abstract record CreateDebtTemplateOutcome
{
    private CreateDebtTemplateOutcome() { }

    public sealed record Success(DebtTemplateDto Created) : CreateDebtTemplateOutcome;

    public sealed record Replayed(DebtTemplateDto Existing) : CreateDebtTemplateOutcome;
}
