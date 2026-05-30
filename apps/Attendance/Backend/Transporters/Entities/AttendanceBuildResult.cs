namespace Backend.Transporters.Entities;

public sealed record AttendanceBuildResult<TEntity>(TEntity Attendance, int MaxStudentLimit)
    where TEntity : class;
