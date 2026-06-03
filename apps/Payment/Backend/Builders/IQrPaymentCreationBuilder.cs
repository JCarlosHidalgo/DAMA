using Backend.Dtos.External.Todotix;
using Backend.Dtos.QrPayments.Output;
using Backend.Entities;
using Backend.Entities.DebtTemplates;
using Backend.Entities.QrPayments;
using Backend.Entities.Todotix;

namespace Backend.Builders;

public interface IQrPaymentCreationBuilder
{
    PendingQrPayment BuildPendingPayment(Guid debtIdentifier, Guid tenantId, Guid studentId, Guid templateId, DebtTemplate template, DateTime expiresAtUtc);

    RegisterDebtRequest BuildTodotixRequest(Guid debtIdentifier, string? email, DebtTemplate template, string tenantTimezone, string description, DateTime expiresAtUtc, string appKey);

    TodotixOutboxEvent BuildOutboxEvent(Guid debtIdentifier, Guid tenantId, RegisterDebtRequest todotixRequest);

    ExpirationOutboxEvent BuildExpirationOutboxEvent(Guid debtIdentifier, Guid tenantId, Guid studentId, DateTime availableAtUtc);

    QrDebtPendingDto BuildPendingDebtDto(Guid debtIdentifier, bool alreadyGenerated = false);
}
