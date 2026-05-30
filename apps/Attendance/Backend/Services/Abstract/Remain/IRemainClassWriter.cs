using Backend.Results.Remain;

namespace Backend.Services.Abstract.Remain;

public interface IRemainClassWriter
{
    Task<IncrementStudentRemainOutcome> IncrementForStudentByClientAsync(Guid requestId, Guid studentId, int quantity, string? studentName);

    Task<IncrementTenantRemainOutcome> IncrementAllInTenantByClientAsync(Guid requestId, int quantity);
}
