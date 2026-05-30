namespace Backend.Dtos.DebtTemplates.Output;

public class DebtTemplateDto
{
    public Guid Id { get; set; }

    public string Description { get; set; } = string.Empty;

    public int ClassQuantity { get; set; }

    public int Cost { get; set; }
}
