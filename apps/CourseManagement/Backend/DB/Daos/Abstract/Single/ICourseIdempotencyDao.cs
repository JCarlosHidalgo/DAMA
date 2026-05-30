using Backend.Entities;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.DB.Daos.Abstract.Single;

public interface ICourseIdempotencyDao
{
    Task<bool> TryRecordAsync(CourseIdempotency record, ITransactionContext transaction);

    Task<CourseIdempotency?> GetByExternalReferenceAsync(Guid tenantId, string externalReference);
}
