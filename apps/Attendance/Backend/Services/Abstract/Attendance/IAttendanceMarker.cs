using Backend.Results.Attendance;
using Backend.Transporters.Entities;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Abstract.Attendance;

public interface IAttendanceMarker
{
    Task<MarkAttendanceOutcome> MarkAsync<TEntity, TResponse>(
        Func<AttendanceMarkContext, Task<AttendanceBuildResult<TEntity>?>> resolveAndBuildAttendance,
        Func<TEntity, ITransactionContext, Task<int>> countOtherStudentsAsync,
        Func<TEntity, ITransactionContext, Task<bool>> tryMarkAttendanceAsync,
        Func<TEntity, string> resolveBroadcastGroup)
        where TEntity : class;
}
