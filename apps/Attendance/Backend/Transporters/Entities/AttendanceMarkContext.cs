namespace Backend.Transporters.Entities;

public sealed record AttendanceMarkContext(Guid TenantId, Guid StudentId, string StudentName, string TenantTimezoneId);
