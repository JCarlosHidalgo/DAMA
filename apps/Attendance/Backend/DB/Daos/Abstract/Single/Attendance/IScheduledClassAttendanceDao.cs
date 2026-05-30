using Backend.Entities.Attendance;

using DAMA.Software.MySqlUnitOfWork;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.Attendance;

public interface IScheduledClassAttendanceDao : IThreeForeignDao<ScheduledClassAttendance>
{
    Task<List<ScheduledClassAttendance>> GetScheduledAttendanceAsync(Guid tenantId, Guid classId, DateOnly classDate);

    Task<List<ScheduledClassAttendance>> GetScheduledAttendanceByStudentIdAsync(Guid tenantId, Guid studentId);

    Task<int> CountByStudentForTenantAsync(Guid tenantId, Guid studentId);

    Task<List<ScheduledClassAttendance>> GetPageByStudentForTenantAsync(Guid tenantId, Guid studentId, int offset, int limit);

    Task<int> CountOtherStudentsForUpdateAsync(Guid tenantId, Guid classId, DateOnly classDate, Guid excludeStudentId, ITransactionContext transaction);

    Task<bool> TryMarkAttendanceAsync(ScheduledClassAttendance attendance, ITransactionContext transaction);

    Task<int> DeleteByClassForTenantAsync(Guid tenantId, Guid classId, ITransactionContext transaction);
}
