using Backend.Dtos.Courses;
using Backend.Entities.Courses;

namespace Backend.Builders;

public interface ICourseBuilder
{
    Course BuildCourse(Guid tenantId, ICourseData payload);
}
