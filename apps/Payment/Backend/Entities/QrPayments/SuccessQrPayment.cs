using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.QrPayments;

public class SuccessQrPayment : IEntity
{
    [Identificator]
    public Guid Id { get; set; }

    [Identifier]
    public Guid TenantId { get; set; }

    [Identifier]
    public Guid StudentId { get; set; }

    [Integer]
    public int ClassQuantity { get; set; }

    [Integer]
    public int Cost { get; set; }

    [Timestamp]
    public DateTime PaidAt { get; set; }
}
