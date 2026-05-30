using Backend.DB.Daos.Abstract.Single.Remain;
using Backend.Results.Attendance;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.Attendance;

internal static class AttendanceRecording
{
    public static async Task<(MarkAttendanceOutcome Result, bool ShouldCommit)> TryRecordAsync(
        Guid tenantId,
        Guid studentId,
        IStudentRemainClassesDao remainClassesDao,
        int maxStudentLimit,
        Func<ITransactionContext, Task<int>> countOtherStudentsAsync,
        Func<ITransactionContext, Task<bool>> tryMarkAsync,
        ITransactionContext transaction)
    {
        if (maxStudentLimit > 0)
        {
            int otherStudents = await countOtherStudentsAsync(transaction);
            if (otherStudents >= maxStudentLimit)
            {
                return (new MarkAttendanceOutcome.ClassFull(), ShouldCommit: false);
            }
        }

        bool decremented = await remainClassesDao.TryDecrementAsync(tenantId, studentId, transaction);
        if (!decremented)
        {
            return (new MarkAttendanceOutcome.NoRemainingClasses(), ShouldCommit: false);
        }

        bool marked = await tryMarkAsync(transaction);
        if (!marked)
        {
            return (new MarkAttendanceOutcome.AlreadyMarked(), ShouldCommit: false);
        }

        return (new MarkAttendanceOutcome.Marked(), ShouldCommit: true);
    }
}
