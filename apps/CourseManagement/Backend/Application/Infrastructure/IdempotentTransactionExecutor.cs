using Backend.DB.Daos.Abstract.Single;
using Backend.Entities;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Application.Infrastructure;

public sealed class IdempotentTransactionExecutor : IIdempotentTransactionExecutor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICourseIdempotencyDao _idempotencyDao;

    public IdempotentTransactionExecutor(IUnitOfWork unitOfWork, ICourseIdempotencyDao idempotencyDao)
    {
        _unitOfWork = unitOfWork;
        _idempotencyDao = idempotencyDao;
    }

    public async Task<IdempotentInsertOutcome<TEntity>> ExecuteAsync<TEntity>(
        Guid tenantId,
        string? externalReference,
        string entityType,
        Guid newEntityId,
        Func<ITransactionContext, Task<TEntity?>> insert,
        Func<Guid, Task<TEntity?>> loadPrior)
        where TEntity : class
    {
        bool duplicateDetected = false;
        TEntity? inserted = null;

        await using (ITransactionScope scope = await _unitOfWork.BeginAsync())
        {
            if (!string.IsNullOrEmpty(externalReference)
                && !await TryRecordLedgerAsync(tenantId, externalReference, entityType, newEntityId, scope))
            {
                duplicateDetected = true;
            }
            else
            {
                inserted = await insert(scope);
                if (inserted != null)
                {
                    await scope.CommitAsync();
                }
            }
        }

        if (duplicateDetected)
        {
            return await ReplayPriorAsync(tenantId, externalReference!, loadPrior);
        }

        if (inserted == null)
        {
            return new IdempotentInsertOutcome<TEntity>.InsertFailed();
        }

        return new IdempotentInsertOutcome<TEntity>.Inserted(inserted);
    }

    private async Task<bool> TryRecordLedgerAsync(
        Guid tenantId,
        string externalReference,
        string entityType,
        Guid newEntityId,
        ITransactionContext transaction)
    {
        CourseIdempotency ledgerRow = new CourseIdempotency
        {
            TenantId = tenantId,
            ExternalReference = externalReference,
            EntityType = entityType,
            EntityId = newEntityId
        };

        return await _idempotencyDao.TryRecordAsync(ledgerRow, transaction);
    }

    private async Task<IdempotentInsertOutcome<TEntity>> ReplayPriorAsync<TEntity>(
        Guid tenantId,
        string externalReference,
        Func<Guid, Task<TEntity?>> loadPrior)
        where TEntity : class
    {
        CourseIdempotency? existing = await _idempotencyDao.GetByExternalReferenceAsync(tenantId, externalReference);
        if (existing == null)
        {
            throw new InvalidOperationException("Idempotency record vanished after duplicate detection.");
        }

        TEntity? prior = await loadPrior(existing.EntityId);
        if (prior == null)
        {
            return new IdempotentInsertOutcome<TEntity>.InsertFailed();
        }
        return new IdempotentInsertOutcome<TEntity>.Replayed(prior);
    }
}
