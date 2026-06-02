namespace Backend.Application.Commands;

public sealed record ProcessQrCallbackCommand(Guid TransactionId, int Error, int CancelOrder);
