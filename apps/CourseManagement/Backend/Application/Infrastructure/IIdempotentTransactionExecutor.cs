using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Application.Infrastructure;

public interface IIdempotentTransactionExecutor
{
    Task<IdempotentInsertOutcome<TEntity>> ExecuteAsync<TEntity>(
        Guid tenantId,
        string? externalReference,
        string entityType,
        Guid newEntityId,
        Func<ITransactionContext, Task<TEntity?>> insert,
        Func<Guid, Task<TEntity?>> loadPrior)
        where TEntity : class;
}
