using Backend.Entities.Users;

namespace Backend.DB.Daos.Abstract.Single.Users;

public interface IUserDirectoryDao
{
    Task<List<User>> GetByRoleForTenantPagedAsync(Guid tenantId, string role, int pageOffset, int pageSize);

    Task<long> CountByRoleForTenantAsync(Guid tenantId, string role);

    Task<User?> GetByIdForTenantAsync(Guid userId, Guid tenantId);

    Task<User?> GetStudentByExactNameForTenantAsync(Guid tenantId, string userName);

    Task<int> SoftDeleteForTenantAsync(Guid userId, Guid tenantId);

    Task<int> TryUpdateUserNameForTenantAsync(Guid userId, Guid tenantId, string newUserName);
}
