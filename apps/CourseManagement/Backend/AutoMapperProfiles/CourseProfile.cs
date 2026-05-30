using AutoMapper;

using Backend.Dtos.Courses.Output;
using Backend.Entities.Courses;

namespace Backend.AutoMapperProfiles;

public class CourseProfile : Profile
{
    public CourseProfile()
    {
        CreateMap<Course, GetCourseDto>();
    }
}
