namespace Backend.Dtos.QrPayments.Output;

public class PendingQrDebtDto : IQrPaymentLine
{
    public Guid Id { get; set; }

    public int ClassQuantity { get; set; }

    public int Cost { get; set; }

    public string? QrImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
