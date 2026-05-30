using Backend.Dtos.Scheduleds;
using Backend.Dtos.Uniques;
using Backend.Entities;
using Backend.Entities.Scheduleds;
using Backend.Entities.Uniques;

namespace Backend.Builders;

public sealed class ClassBuilder : IClassBuilder
{
    public UniqueClass BuildUniqueClass(Guid tenantId, Guid courseId, IUniqueClassPayload payload, List<ClassTeacher> teachers)
    {
        return new UniqueClass
        {
            Id = Guid.NewGuid(),
            Date = payload.Date,
            MaxStudentLimit = payload.MaxStudentLimit,
            StartTime = payload.StartTime,
            EndTime = payload.EndTime,
            CourseId = courseId,
            TenantId = tenantId,
            Teachers = teachers
        };
    }

    public ScheduledClass BuildScheduledClass(Guid tenantId, Guid courseId, IScheduledClassPayload payload, List<ClassTeacher> teachers)
    {
        return new ScheduledClass
        {
            Id = Guid.NewGuid(),
            DayOfWeekIndex = payload.DayOfWeekIndex,
            MaxStudentLimit = payload.MaxStudentLimit,
            StartTime = payload.StartTime,
            EndTime = payload.EndTime,
            CourseId = courseId,
            TenantId = tenantId,
            Teachers = teachers
        };
    }
}
