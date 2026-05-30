using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Remain;
using Backend.Results.Remain;
using Backend.Services.Abstract.Remain;

using DAMA.Software.MySqlOutbox;
using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.Remain;

public sealed class RemainClassWriter(IStudentRemainClassesDao remainClassesDao,
                                       IRemainRequestDao remainRequestDao,
                                       IUnitOfWork unitOfWork,
                                       IClaimContext claimContext) : IRemainClassWriter
{
    public async Task<IncrementStudentRemainOutcome> IncrementForStudentByClientAsync(
        Guid requestId,
        Guid studentId,
        int quantity,
        string? studentName)
    {
        Guid tenantId = claimContext.TenantId;

        return await IdempotentTransaction.RunAsync<IncrementStudentRemainOutcome>(
            unitOfWork,
            remainRequestDao,
            requestId,
            new IncrementStudentRemainOutcome.AlreadyApplied(),
            async scope =>
            {
                await remainClassesDao.IncrementAsync(tenantId, studentId, quantity, studentName, scope);
                return new IncrementStudentRemainOutcome.Applied();
            });
    }

    public async Task<IncrementTenantRemainOutcome> IncrementAllInTenantByClientAsync(Guid requestId, int quantity)
    {
        Guid tenantId = claimContext.TenantId;

        return await IdempotentTransaction.RunAsync<IncrementTenantRemainOutcome>(
            unitOfWork,
            remainRequestDao,
            requestId,
            new IncrementTenantRemainOutcome.AlreadyApplied(),
            async scope =>
            {
                int affected = await remainClassesDao.IncrementAllInTenantAsync(tenantId, quantity, scope);
                return new IncrementTenantRemainOutcome.Applied(affected);
            });
    }
}
