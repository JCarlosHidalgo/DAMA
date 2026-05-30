namespace Backend.Dtos.DebtTemplates.Input;

public class UpdateDebtTemplateDto : IDebtTemplateData
{
    public required string Description { get; set; }

    public required int ClassQuantity { get; set; }

    public required int Cost { get; set; }
}
