namespace Backend.Results.DebtTemplates;

public abstract record DeleteDebtTemplateOutcome
{
    private DeleteDebtTemplateOutcome() { }

    public sealed record Deleted : DeleteDebtTemplateOutcome;

    public sealed record NotFound : DeleteDebtTemplateOutcome;
}
