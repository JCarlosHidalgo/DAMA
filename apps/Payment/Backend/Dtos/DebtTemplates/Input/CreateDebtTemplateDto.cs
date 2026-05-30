namespace Backend.Dtos.DebtTemplates.Input;

public class CreateDebtTemplateDto : IDebtTemplateData
{
    public required string Description { get; set; }

    public required int ClassQuantity { get; set; }

    public required int Cost { get; set; }

    public string? ExternalReference { get; set; }
}
