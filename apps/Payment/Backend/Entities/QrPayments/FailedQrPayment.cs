using SQLDaosPackage.Entities;
using SQLDaosPackage.Entities.Attributes;

namespace Backend.Entities.QrPayments;

public class FailedQrPayment : IEntity
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

    [Text(3)]
    public string Currency { get; set; } = "BOB";

    [Timestamp]
    public DateTime FailedAt { get; set; }

    public FailureReason FailureReason { get; set; } = FailureReason.CallbackError;
}
