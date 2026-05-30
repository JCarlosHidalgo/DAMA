using AutoMapper;

using Backend.Dtos.Uniques.Output;
using Backend.Entities.Uniques;

namespace Backend.AutoMapperProfiles;

public class UniqueClassProfile : Profile
{
    public UniqueClassProfile()
    {
        CreateMap<UniqueClass, GetUniqueClassDto>();
    }
}
