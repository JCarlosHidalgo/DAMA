using System.Diagnostics;

using Backend.DB.Daos.Abstract.Single.Courses;
using Backend.Entities;

namespace Backend.Application.Infrastructure;

public sealed class ClassCreationCoordinator<TEntity> : IClassCreationCoordinator<TEntity>
    where TEntity : class
{
    private readonly ICourseDao _courseDao;
    private readonly IIdempotentTransactionExecutor _idempotentExecutor;
    private readonly IClassAggregateWriter<TEntity> _writer;

    public ClassCreationCoordinator(ICourseDao courseDao,
                                    IIdempotentTransactionExecutor idempotentExecutor,
                                    IClassAggregateWriter<TEntity> writer)
    {
        _courseDao = courseDao;
        _idempotentExecutor = idempotentExecutor;
        _writer = writer;
    }

    public async Task<ClassCreationOutcome<TEntity>> CreateAsync(
        Guid tenantId,
        Guid courseId,
        string? externalReference,
        string entityType,
        Guid newEntityId,
        TEntity entity,
        IReadOnlyList<ClassTeacher> teachers)
    {
        if (!await _courseDao.ExistsForTenantAsync(tenantId, courseId))
        {
            return new ClassCreationOutcome<TEntity>.CourseMissing();
        }

        IdempotentInsertOutcome<TEntity> outcome = await _idempotentExecutor.ExecuteAsync<TEntity>(
            tenantId,
            externalReference,
            entityType,
            newEntityId,
            async transaction =>
            {
                if (!await _writer.CreateForTenantAsync(entity, tenantId, transaction))
                {
                    return null;
                }
                foreach (ClassTeacher teacher in teachers)
                {
                    await _writer.InsertTeacherAsync(newEntityId, teacher, tenantId, transaction);
                }
                return entity;
            },
            entityId => _writer.GetByIdForTenantAsync(tenantId, entityId));

        return outcome switch
        {
            IdempotentInsertOutcome<TEntity>.Inserted inserted => new ClassCreationOutcome<TEntity>.Created(inserted.Entity),
            IdempotentInsertOutcome<TEntity>.Replayed replayed => new ClassCreationOutcome<TEntity>.Replayed(replayed.Prior),
            _ => throw new UnreachableException("Course existence was verified before invoking the executor.")
        };
    }
}
