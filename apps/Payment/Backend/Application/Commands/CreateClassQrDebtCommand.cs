using Backend.Dtos.QrPayments.Input;

namespace Backend.Application.Commands;

public sealed record CreateClassQrDebtCommand(Guid TemplateId, string? Email, CreateQrDebtDto Dto);
