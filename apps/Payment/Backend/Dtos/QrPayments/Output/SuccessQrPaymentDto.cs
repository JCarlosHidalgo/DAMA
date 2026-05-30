namespace Backend.Dtos.QrPayments.Output;

public class SuccessQrPaymentDto : IQrPaymentLine
{
    public Guid Id { get; set; }

    public int ClassQuantity { get; set; }

    public int Cost { get; set; }

    public DateTime PaidAt { get; set; }
}
