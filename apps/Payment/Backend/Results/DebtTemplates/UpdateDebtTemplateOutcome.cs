namespace Backend.Results.DebtTemplates;

public abstract record UpdateDebtTemplateOutcome
{
    private UpdateDebtTemplateOutcome() { }

    public sealed record Updated : UpdateDebtTemplateOutcome;

    public sealed record NotFound : UpdateDebtTemplateOutcome;
}
