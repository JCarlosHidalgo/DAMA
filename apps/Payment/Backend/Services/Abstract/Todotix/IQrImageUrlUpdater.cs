using Backend.Entities;

namespace Backend.Services.Abstract.Todotix;

public interface IQrImageUrlUpdater
{
    DebtKind Kind { get; }

    Task UpdateAsync(Guid pendingId, string qrImageUrl);
}
