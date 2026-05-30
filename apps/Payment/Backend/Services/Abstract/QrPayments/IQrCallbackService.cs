namespace Backend.Services.Abstract.QrPayments;

public interface IQrCallbackService
{
    Task HandleCallbackAsync(Guid transactionId, int error, int cancelOrder);
}
