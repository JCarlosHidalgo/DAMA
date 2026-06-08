namespace Backend.Dtos.QrPayments.Output;

public class FailedQrPaymentDto : IQrPaymentLine
{
    public Guid Id { get; set; }

    public int ClassQuantity { get; set; }

    public int Cost { get; set; }

    public string Currency { get; set; } = "BOB";

    public DateTime FailedAt { get; set; }

    public string FailureReason { get; set; } = nameof(Entities.QrPayments.FailureReason.CallbackError);
}
