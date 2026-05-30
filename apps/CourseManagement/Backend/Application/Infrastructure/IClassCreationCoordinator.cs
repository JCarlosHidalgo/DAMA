using Backend.Entities;

namespace Backend.Application.Infrastructure;

public interface IClassCreationCoordinator<TEntity> where TEntity : class
{
    Task<ClassCreationOutcome<TEntity>> CreateAsync(
        Guid tenantId,
        Guid courseId,
        string? externalReference,
        string entityType,
        Guid newEntityId,
        TEntity entity,
        IReadOnlyList<ClassTeacher> teachers);
}
