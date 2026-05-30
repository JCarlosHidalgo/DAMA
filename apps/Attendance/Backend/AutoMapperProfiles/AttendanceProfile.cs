using AutoMapper;

using Backend.Dtos.Attendance.Output;
using Backend.Dtos.Remain.Output;
using Backend.Entities.Attendance;
using Backend.Entities.Remain;

namespace Backend.AutoMapperProfiles;

public class AttendanceProfile : Profile
{
    public AttendanceProfile()
    {
        CreateMap<ScheduledClassAttendance, ScheduledAttendanceResponse>();
        CreateMap<UniqueClassAttendance, UniqueAttendanceResponse>();
        CreateMap<StudentRemainClasses, RemainResponse>()
            .ForMember(destination => destination.StudentId, options => options.MapFrom(source => source.Id));
    }
}
