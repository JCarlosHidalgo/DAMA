using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.PaymentCredentials;

public class TenantPaymentCredential : IEntity
{
    [Identificator]
    public Guid TenantId { get; set; }

    [Text(512)]
    public string TodotixAppKey { get; set; } = string.Empty;
}
