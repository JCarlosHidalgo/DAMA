using Backend.Dtos.DebtTemplates;
using Backend.Entities.DebtTemplates;
using Backend.Entities.QrPayments;

namespace Backend.Builders;

public interface IDebtTemplateBuilder
{
    DebtTemplate BuildDebtTemplate(Guid tenantId, IDebtTemplateData dto);

    QrPaymentIdempotency BuildIdempotencyRecord(Guid tenantId, string externalReference, Guid entityId);
}
