using AutoMapper;

using Backend.Dtos.Scheduleds.Output;
using Backend.Entities.Scheduleds;

namespace Backend.AutoMapperProfiles;

public class ScheduledClassProfile : Profile
{
    public ScheduledClassProfile()
    {
        CreateMap<ScheduledClass, GetScheduledClassDto>();
    }
}
