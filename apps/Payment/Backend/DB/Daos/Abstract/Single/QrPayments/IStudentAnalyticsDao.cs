namespace Backend.DB.Daos.Abstract.Single.QrPayments;

public readonly record struct StudentQrBreakdownRow(
    int PendingCount,
    int PendingAmount,
    int SuccessCount,
    int SuccessAmount,
    int ExpiredCount,
    int ExpiredAmount,
    int OtherFailedCount,
    int OtherFailedAmount);

public readonly record struct StudentSpendMonthRow(
    int Year,
    int Month,
    int Amount,
    int Count);

public interface IStudentAnalyticsDao
{
    Task<StudentQrBreakdownRow> GetStatusBreakdownAsync(Guid tenantId, Guid studentId);

    Task<List<StudentSpendMonthRow>> GetSpendByMonthAsync(Guid tenantId, Guid studentId, DateTime fromDate, DateTime toDate);
}
