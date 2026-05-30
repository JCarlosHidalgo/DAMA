using Backend.Dtos.QrPayments.Input;
using Backend.Results.QrPayments;

namespace Backend.Services.Abstract.QrPayments;

public interface IQrDebtCreationService
{
    Task<CreateQrDebtOutcome> CreateDebtAsync(Guid templateId, string? email, CreateQrDebtDto dto);
}
