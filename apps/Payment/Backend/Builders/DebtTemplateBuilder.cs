using Backend.Dtos.DebtTemplates;
using Backend.Entities.DebtTemplates;
using Backend.Entities.QrPayments;
using Backend.Options;

using Microsoft.Extensions.Options;

namespace Backend.Builders;

public class DebtTemplateBuilder : IDebtTemplateBuilder
{
    private readonly IOptions<CurrencyOptions> _currencyOptions;

    public DebtTemplateBuilder(IOptions<CurrencyOptions> currencyOptions)
    {
        _currencyOptions = currencyOptions;
    }

    public DebtTemplate BuildDebtTemplate(Guid tenantId, IDebtTemplateData dto)
    {
        return new DebtTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Description = dto.Description,
            ClassQuantity = dto.ClassQuantity,
            Cost = dto.Cost,
            Currency = _currencyOptions.Value.Default
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
