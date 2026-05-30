namespace Backend.Dtos.QrPayments.Input;

public class CreateQrDebtDto
{
    public string? Email { get; set; }

    public string? ExternalReference { get; set; }

    public string? Descripcion { get; set; }
}
