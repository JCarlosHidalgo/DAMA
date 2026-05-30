namespace Backend.Dtos.DebtTemplates;

public interface IDebtTemplateData
{
    string Description { get; }

    int ClassQuantity { get; }

    int Cost { get; }
}
