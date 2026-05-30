using Backend.Entities;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Application.Infrastructure;

public interface IClassAggregateWriter<TEntity> where TEntity : class
{
    Task<bool> CreateForTenantAsync(TEntity entity, Guid tenantId, ITransactionContext transaction);

    Task InsertTeacherAsync(Guid entityId, ClassTeacher teacher, Guid tenantId, ITransactionContext transaction);

    Task<TEntity?> GetByIdForTenantAsync(Guid tenantId, Guid id);
}
