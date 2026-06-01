using Backend.Entities.Groups;

using DAMA.Software.MySqlUnitOfWork;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.Groups;

public interface IClassGroupDao : ISingleDao<ClassGroup>
{
    Task CreateForTenantAsync(ClassGroup classGroup);

    Task<bool> UpdateForTenantAsync(Guid tenantId, Guid id, string newName);

    Task<bool> DeleteForTenantIfEmptyAsync(Guid tenantId, Guid id);

    Task<List<ClassGroup>> GetByTenantAsync(Guid tenantId);

    Task<List<ClassGroup>> GetByTeacherForTenantAsync(Guid tenantId, Guid teacherId);

    Task<bool> ExistsForTenantAsync(Guid tenantId, Guid id);
}
