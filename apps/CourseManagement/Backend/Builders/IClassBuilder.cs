using Backend.Dtos.Scheduleds;
using Backend.Dtos.Uniques;
using Backend.Entities;
using Backend.Entities.Scheduleds;
using Backend.Entities.Uniques;

namespace Backend.Builders;

public interface IClassBuilder
{
    UniqueClass BuildUniqueClass(Guid tenantId, Guid courseId, Guid groupId, IUniqueClassPayload payload, List<ClassTeacher> teachers);

    ScheduledClass BuildScheduledClass(Guid tenantId, Guid courseId, Guid groupId, IScheduledClassPayload payload, List<ClassTeacher> teachers);
}
