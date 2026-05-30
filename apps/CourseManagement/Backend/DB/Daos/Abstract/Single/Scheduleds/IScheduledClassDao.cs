using Backend.Application.Infrastructure;
using Backend.Entities;
using Backend.Entities.Scheduleds;
using Backend.Results;

using DAMA.Software.MySqlUnitOfWork;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.Scheduleds;

public interface IScheduledClassDao : ISingleDao<ScheduledClass>, IClassAggregateWriter<ScheduledClass>
{
    Task ReplaceTeachersAsync(Guid scheduledClassId, IReadOnlyList<ClassTeacher> teachers, Guid tenantId, ITransactionContext transaction);

    Task<List<ScheduledClass>> GetScheduledClassesByCourseIdAsync(Guid courseId);

    Task<List<ScheduledClass>> GetByTenantAsync(Guid tenantId);

    Task<List<ScheduledClass>> GetByTeacherForTenantAsync(Guid tenantId, Guid teacherId);

    Task<ClassExistenceMeta?> FindForTenantAsync(Guid tenantId, Guid classId, DateOnly classDate);

    Task<bool> UpdateForTenantAsync(ScheduledClassUpdate scheduledClassUpdate, Guid tenantId, ITransactionContext transaction);

    Task<bool> HasOverlapForTeacherAsync(Guid tenantId, Guid teacherId, int dayOfWeekIndex, TimeOnly startTime, TimeOnly endTime, Guid? excludeId);

    Task<bool> DeleteForTenantAsync(Guid tenantId, Guid id, ITransactionContext transaction);

    Task<List<Guid>> GetIdsByCourseForTenantAsync(Guid tenantId, Guid courseId, ITransactionContext transaction);
}
