namespace Backend.Dtos.QrPayments;

public interface IQrDebtState
{
    Guid IdentificadorDeuda { get; }

    string Status { get; }
}
