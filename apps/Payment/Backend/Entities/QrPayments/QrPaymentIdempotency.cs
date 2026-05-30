using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.QrPayments;

public class QrPaymentIdempotency
{
    [Identifier]
    public Guid TenantId { get; set; }

    [Text(128)]
    public string ExternalReference { get; set; } = string.Empty;

    [Identifier]
    public Guid EntityId { get; set; }
}
