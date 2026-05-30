namespace Backend.Dtos.QrPayments;

public interface IQrPaymentLine
{
    Guid Id { get; }

    int ClassQuantity { get; }

    int Cost { get; }
}
