using Backend.Entities.Attendance;

using DAMA.Software.MySqlUnitOfWork;

using SQLDaosPackage.Daos;

namespace Backend.DB.Daos.Abstract.Single.Attendance;

public interface IUniqueClassAttendanceDao : IThreeForeignDao<UniqueClassAttendance>
{
    Task<List<UniqueClassAttendance>> GetUniqueAttendanceAsync(Guid tenantId, Guid classId);

    Task<List<UniqueClassAttendance>> GetUniqueAttendanceByStudentIdAsync(Guid tenantId, Guid studentId);

    Task<int> CountByStudentForTenantAsync(Guid tenantId, Guid studentId);

    Task<List<UniqueClassAttendance>> GetPageByStudentForTenantAsync(Guid tenantId, Guid studentId, int offset, int limit);

    Task<int> CountOtherStudentsForUpdateAsync(Guid tenantId, Guid classId, Guid excludeStudentId, ITransactionContext transaction);

    Task<bool> TryMarkAttendanceAsync(UniqueClassAttendance attendance, ITransactionContext transaction);

    Task<int> DeleteByClassForTenantAsync(Guid tenantId, Guid classId, ITransactionContext transaction);
}
