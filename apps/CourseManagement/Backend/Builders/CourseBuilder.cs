using Backend.Dtos.Courses;
using Backend.Entities.Courses;

namespace Backend.Builders;

public sealed class CourseBuilder : ICourseBuilder
{
    public Course BuildCourse(Guid tenantId, ICourseData payload)
    {
        return new Course
        {
            Id = Guid.NewGuid(),
            Name = payload.Name,
            TenantId = tenantId
        };
    }
}
