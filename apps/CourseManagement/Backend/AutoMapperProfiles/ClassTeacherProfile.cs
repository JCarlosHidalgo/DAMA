using AutoMapper;

using Backend.Dtos;
using Backend.Entities;

namespace Backend.AutoMapperProfiles;

public class ClassTeacherProfile : Profile
{
    public ClassTeacherProfile()
    {
        CreateMap<ClassTeacher, ClassTeacherDto>();
        CreateMap<ClassTeacherDto, ClassTeacher>();
    }
}
