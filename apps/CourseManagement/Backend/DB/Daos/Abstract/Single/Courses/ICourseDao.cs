using Backend.Entities.Courses;

using DAMA.Software.MySqlUnitOfWork;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.Courses;

public interface ICourseDao : ISingleDao<Course>
{
    new Task CreateAsync(Course course);

    Task CreateAsync(Course course, ITransactionContext transaction);

    Task<List<Course>> GetCoursesByTenantIdAsync(Guid tenantId);

    Task<Course?> GetByIdForTenantAsync(Guid tenantId, Guid courseId);

    Task<bool> ExistsForTenantAsync(Guid tenantId, Guid courseId);

    Task<bool> UpdateForTenantAsync(Guid tenantId, Guid courseId, string newName);

    Task<bool> DeleteForTenantAsync(Guid tenantId, Guid courseId, ITransactionContext transaction);
}
