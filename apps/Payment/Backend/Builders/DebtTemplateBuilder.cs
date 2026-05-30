using Backend.Dtos.DebtTemplates;
using Backend.Entities.DebtTemplates;
using Backend.Entities.QrPayments;

namespace Backend.Builders;

public class DebtTemplateBuilder : IDebtTemplateBuilder
{
    public DebtTemplate BuildDebtTemplate(Guid tenantId, IDebtTemplateData dto)
    {
        return new DebtTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Description = dto.Description,
            ClassQuantity = dto.ClassQuantity,
            Cost = dto.Cost
        };
    }

    public QrPaymentIdempotency BuildIdempotencyRecord(Guid tenantId, string externalReference, Guid entityId)
    {
        return new QrPaymentIdempotency
        {
            TenantId = tenantId,
            ExternalReference = externalReference,
            EntityId = entityId
        };
    }
}
