using AutoMapper;

using Backend.Dtos.Groups.Output;
using Backend.Entities.Groups;

namespace Backend.AutoMapperProfiles;

public class ClassGroupProfile : Profile
{
    public ClassGroupProfile()
    {
        CreateMap<ClassGroup, GetClassGroupDto>();
    }
}
