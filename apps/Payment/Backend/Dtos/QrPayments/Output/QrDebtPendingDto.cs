namespace Backend.Dtos.QrPayments.Output;

public class QrDebtPendingDto : IQrDebtState
{
    public Guid IdentificadorDeuda { get; set; }

    public string Status { get; set; } = "Pending";

    public bool AlreadyGenerated { get; set; }
}
