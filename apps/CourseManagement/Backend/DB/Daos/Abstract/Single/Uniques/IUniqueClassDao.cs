using Backend.Application.Infrastructure;
using Backend.Entities;
using Backend.Entities.Uniques;
using Backend.Results;

using DAMA.Software.MySqlUnitOfWork;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.Uniques;

public interface IUniqueClassDao : ISingleDao<UniqueClass>, IClassAggregateWriter<UniqueClass>
{
    Task ReplaceTeachersAsync(Guid uniqueClassId, IReadOnlyList<ClassTeacher> teachers, Guid tenantId, ITransactionContext transaction);

    Task<List<UniqueClass>> GetUniqueClassesOnSameWeekByDateAsync(Guid courseId, DateOnly classDate);

    Task<List<UniqueClass>> GetOnWeekForTenantAsync(Guid tenantId, DateOnly classDate);

    Task<List<UniqueClass>> GetByTeacherOnWeekForTenantAsync(Guid tenantId, Guid teacherId, DateOnly classDate);

    Task<List<UniqueClass>> GetByTeacherForTenantAsync(Guid tenantId, Guid teacherId);

    Task<ClassExistenceMeta?> FindForTenantAsync(Guid tenantId, Guid classId);

    Task<bool> UpdateForTenantAsync(UniqueClassUpdate uniqueClassUpdate, Guid tenantId, ITransactionContext transaction);

    Task<bool> HasGroupOverlapAsync(Guid tenantId, Guid groupId, DateOnly classDate, TimeOnly startTime, TimeOnly endTime, Guid? excludeId);

    Task<bool> TransferToGroupAsync(Guid tenantId, Guid id, Guid targetGroupId, ITransactionContext transaction);

    Task<bool> DeleteForTenantAsync(Guid tenantId, Guid id, ITransactionContext transaction);

    Task<List<Guid>> GetIdsByCourseForTenantAsync(Guid tenantId, Guid courseId, ITransactionContext transaction);
}
