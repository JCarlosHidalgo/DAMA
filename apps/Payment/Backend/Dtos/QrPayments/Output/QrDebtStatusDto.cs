namespace Backend.Dtos.QrPayments.Output;

public class QrDebtStatusDto : IQrDebtState
{
    public Guid IdentificadorDeuda { get; set; }

    public string Status { get; set; } = "Pending";

    public string? QrSimpleUrl { get; set; }

    public string? Error { get; set; }
}
