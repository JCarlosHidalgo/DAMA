using Backend.Entities;

namespace Backend.Builders;

public interface ICourseEventBuilder
{
    OutboxEvent BuildCourseDeleted(Guid tenantId, Guid courseId, IReadOnlyList<Guid> classIds);

    OutboxEvent BuildClassDeleted(Guid tenantId, Guid classId);
}
